﻿using System;
using System.Collections.Generic;

namespace Singularity.Graph.Resolvers
{
    internal interface IResolverPipeline
    {
        Dependency? TryGetDependency(Type type);
        Dependency GetDependency(Type type);
        void ResolveDependency(ResolvedDependency dependency);
        IReadOnlyDictionary<Type, Dependency> Dependencies { get; }
        object SyncRoot { get; }
    }
}