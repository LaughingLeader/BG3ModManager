﻿using Splat;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager
{
	public static class Services
	{
		public static T Get<T>(string contract = null)
		{
			return Locator.Current.GetService<T>(contract);
		}

		public static void Register<T>(Func<object> constructorCallback, string contract = null)
		{
			Locator.CurrentMutable.Register(constructorCallback, typeof(T), contract);
		}

		public static void RegisterSingleton<T>(T instance, string contract = null)
		{
			Locator.CurrentMutable.RegisterConstant(instance, typeof(T), contract);
		}

		/// <summary>
		/// Register a singleton which won't get created until the first user accesses it.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="constructorCallback"></param>
		/// <param name="contract"></param>
		public static void RegisterLazySingleton<T>(Func<object> constructorCallback, string contract = null)
		{
			Locator.CurrentMutable.RegisterLazySingleton(constructorCallback, typeof(T), contract);
		}
	}
}
