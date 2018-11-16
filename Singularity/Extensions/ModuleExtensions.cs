﻿using System.Collections.Generic;
using Singularity.Bindings;
using Singularity.Graph;

namespace Singularity
{
	internal static class ModuleExtensions
	{
		public static IEnumerable<Binding> ToBindings(this IEnumerable<IModule> modules)
		{
			var config = new BindingConfig();
			foreach (IModule module in modules)
			{
				config.CurrentModule = module;
				module.Register(config);
			}

			config.CurrentModule = null;
			return config;
		}
	}
}
