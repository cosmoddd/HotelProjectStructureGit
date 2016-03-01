using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Polybrush
{
	/**
	 *	Static helper methods for working with reflection.  Mostly used for ProBuilder
	 *	compatibility.
	 */
	public static class z_ReflectionUtil
	{
		static EditorWindow _pbEditor = null;

		/**
		 *	Reference to the ProBuilder Editor window if it is avaiable.
		 */
		public static EditorWindow pbEditor
		{
			get
			{
				if(_pbEditor == null)
				{
					EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
					_pbEditor = windows.FirstOrDefault(x => x.GetType().ToString().Contains("pb_Editor"));
				}
				return _pbEditor;
			}
		}

		/**
		 *	Tests if ProBuilder is available in the project.
		 */
		public static bool ProBuilderExists()
		{
			return AppDomain.CurrentDomain.GetAssemblies().Any(x => x.FullName.Contains(z_Pref.PB_EDITOR_ASSEMBLY));
		}

		/**
		 *	Tests if a GameObject is a ProBuilder mesh or not.
		 */
		public static bool IsProBuilderObject(GameObject gameObject)
		{
			return gameObject != null && gameObject.GetComponent("pb_Object") != null;
		}

		/**
		 *	Get a component with type name.
		 */
		public static object GetComponent(this GameObject gameObject, string componentTypeName)
		{
			return gameObject.GetComponent(componentTypeName);
		}

		/**
		 *	Fetch a type with name and optional assembly name.
		 */
		public static Type GetType(string type, string assembly = null)
		{
			Type t = Type.GetType(type);

			if(t == null)
			{
				IEnumerable<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies();

				if(assembly != null)
					assemblies = assemblies.Where(x => x.FullName.Contains(assembly));

				foreach(Assembly ass in assemblies)
				{
					t = ass.GetType(type);

					if(t != null)
						return t;
				}
			}

			return t;
		}

		/**
		 *	Call a method with args.
		 */
		public static object Invoke(object target,
									string method,
									BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
									params object[] args)
		{
			if(target == null)
			{
				Debug.LogWarning("Invoke failed, target is null and no type was provided.");
				return null;
			}

			return Invoke(target, target.GetType(), method, null, flags, args);
		}

		public static object Invoke(object target,
									string type,
									string method,
									BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
									string assembly = null,
									params object[] args)
		{
			Type t = GetType(type, assembly);

			if(t == null && target != null)
				t = target.GetType();

			if(t != null)
				return Invoke(target, t, method, null, flags, args);
			else
				Debug.LogWarning("Invoke failed, type is null: " + type);

			return null;
		}

		public static object Invoke(object target,
									Type type,
									string method,
									Type[] methodParams = null,
									BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
									params object[] args)
		{
			MethodInfo mi = null;

			if(methodParams == null)
				mi = type.GetMethod(method, flags);
			else
				mi = type.GetMethod(method, flags, null, methodParams, null);

			if(mi == null)
			{
				Debug.LogWarning("Failed to find method " + method + " in type " + type);
				return null;
			}

			return mi.Invoke(target, args);
		}

		/**
		 *	Fetch a value using GetProperty or GetField.
		 */
		public static object GetValue(object target, string type, string member, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
		{
			Type t = GetType(type);

			if(t == null)
			{
				Debug.LogWarning("Could not find type \"" + type + "\"!");
				return null;
			}
			else
				return GetValue(target, t, member, flags);
		}

		public static object GetValue(object target, Type type, string member, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
		{
			PropertyInfo pi = type.GetProperty(member, flags);

			if(pi != null)
				return pi.GetValue(target, null);

			FieldInfo fi = type.GetField(member, flags);

			if(fi != null)
				return fi.GetValue(target);

			return null;
		}
	}
}
