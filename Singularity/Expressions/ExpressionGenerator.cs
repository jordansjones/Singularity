﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;
using Singularity.Graph;

namespace Singularity.Expressions
{
    internal class ExpressionGenerator
    {
        public static ParameterExpression ScopeParameter = Expression.Parameter(typeof(Scoped));
        private static readonly MethodInfo GenericAddMethod = typeof(Scoped).GetRuntimeMethods().FirstOrDefault(x => x.Name == nameof(Scoped.Add));

        public Expression GenerateDependencyExpression(ResolvedDependency dependency, Scoped graphScope)
        {
            Expression expression = dependency.Binding.Expression! is LambdaExpression lambdaExpression ? lambdaExpression.Body : dependency.Binding.Expression;
            var parameterExpressionVisitor = new ParameterExpressionVisitor(dependency.Children);
            expression = parameterExpressionVisitor.Visit(expression);

            if (dependency.Binding.OnDeathAction != null)
            {
                MethodInfo method = GenericAddMethod.MakeGenericMethod(expression.Type);
                expression = Expression.Call(ScopeParameter, method, expression, Expression.Constant(dependency.Binding));
            }

            if (dependency.Registration.Decorators.Count > 0)
            {
                var body = new List<Expression>();
                ParameterExpression instanceParameter = Expression.Variable(dependency.Registration.DependencyType, $"{expression.Type} instance");
                body.Add(Expression.Assign(instanceParameter, Expression.Convert(expression, dependency.Registration.DependencyType)));

                if (dependency.Registration.Decorators.Count > 0)
                {
                    var decoratorExpressionVisitor = new DecoratorExpressionVisitor(dependency.Children, instanceParameter.Type);
                    decoratorExpressionVisitor.PreviousDecorator = instanceParameter;
                    foreach (Expression decorator in dependency.Registration.Decorators)
                    {
                        Expression decoratorExpression = decorator;

                        decoratorExpression = decoratorExpressionVisitor.Visit(decoratorExpression);

                        decoratorExpressionVisitor.PreviousDecorator = decoratorExpression;
                    }
                    body.Add(decoratorExpressionVisitor.PreviousDecorator);
                }

                if (body.Last().Type == typeof(void)) body.Add(instanceParameter);
                expression = body.Count == 1 ? expression : Expression.Block(new[] { instanceParameter }, body);
            }

            if (dependency.Binding.CreationMode is CreationMode.Singleton)
            {
                object value;
                if (expression is NewExpression newExpression && newExpression.Arguments.Count == 0)
                {
                    //In this case we know the signature and can call the constructor directly instead of doing a costly compile.
                    value = newExpression.Constructor.Invoke(null);
                }
                else
                {
                    value = ((Func<Scoped, object>)Expression.Lambda(expression, ScopeParameter).CompileFast()).Invoke(graphScope);
                }
                dependency.InstanceFactory = scope => value;
                return Expression.Constant(value, dependency.Registration.DependencyType);
            }
            return expression;
        }
    }
}
