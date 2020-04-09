﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Singularity.Resolving.Generators
{
    /// <summary>
    /// Creates a binding if the type is a concrete type.
    /// </summary>
    public sealed class ConcreteServiceBindingGenerator : IGenericServiceGenerator
    {
        private static Type[] _excludedGenericTypes = new Type[] { typeof(HashSet<>), typeof(List<>), typeof(Lazy<>) };
        private static Type[] _excludedTypes = new Type[] { typeof(Scoped), typeof(Container), typeof(string), typeof(object) };
        public bool CanResolve(Type type)
        {
            return !type.IsInterface && 
                !type.IsArray && 
                !type.IsAbstract && 
                !type.IsPrimitive && 
                !type.IsSubclassOf(typeof(Delegate)) &&
                !_excludedTypes.Contains(type) &&
                !(type.IsGenericType && _excludedGenericTypes.Contains(type.GetGenericTypeDefinition())) && 
                type.GetConstructorCandidates().Any();
        }

        public IEnumerable<ServiceBinding> Wrap<TTarget>(IInstanceFactoryResolver resolver)
        {
            var expression = resolver.Settings.ConstructorResolver.ResolveConstructorExpression(typeof(TTarget));
            yield return new ServiceBinding(typeof(TTarget), BindingMetadata.GeneratedInstance, expression, typeof(TTarget), ConstructorResolvers.Default, Lifetimes.Transient);
        }
    }
}