﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using Nodeplay.Utils;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;


namespace Nodeplay.UI
{
	/// <summary>
	/// TODO may design this class as generic so we can keep track of the type
	/// and have it work with a monobehavior based class that points here
	/// </summary>
	[RequireComponent(typeof(LayoutElement))]
	[RequireComponent(typeof(EventConsumer))]
	//[RequireComponent(typeof(HorizontalLayoutGroup))]
	public class InspectableElement: UIBehaviour ,IPointerClickHandler
	{
		public NodeModel Model;
		public Type ElementType;
		public object Reference;
		private bool exposesubElements = false;
		public string Name;
		// Use this for initialization
		protected override void Start()
		{
			Model = this.transform.root.GetComponentInChildren<NodeModel>();
			

		}

		public void UpdateText(object pointer = null)
		{
			if (pointer == null)
			{
				GetComponentInChildren<Text>().text = ElementType.ToString() + " : " + Name +" : " + Reference.ToString(); 
			}
			else
			{
			GetComponentInChildren<Text>().text = pointer.GetType().ToString() + " : " + pointer.ToString(); 
			}
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (exposesubElements == false){
				exposesubElements = true;
				populateNextLevel(this.Reference);

			}
			else{

				var childrenRoot = transform.parent.GetChild(1);
				GameObject.DestroyImmediate(childrenRoot.gameObject);
				exposesubElements = false;
			}
		}


		private void populateNextLevel(System.Object subTreeRoot)
		{

			var wrapper = new GameObject("sub_tree_wrapper");
			wrapper.transform.position = this.transform.position;
			wrapper.transform.SetParent(this.transform.parent,false);
			wrapper.AddComponent<HorizontalLayoutGroup>();
			

			if (InspectorVisualization.IsList(subTreeRoot))
			{
				Debug.Log("inputobject is a list");
				foreach (var item in (IEnumerable)subTreeRoot)
				{
					var inspectabelgo = InspectorVisualization.generateInspectableElementGameObject(item,wrapper);

				}
			}
			
			else if (InspectorVisualization.IsDictionary(subTreeRoot))
			{
				Debug.Log("inputobject is a dictionary");
				foreach (var pair in (IEnumerable)subTreeRoot)
				{
					var realpair = DictionaryHelpers.CastFrom(pair);
					var key = realpair.Key;
					var value = realpair.Value;
					
					InspectorVisualization.generateInspectableElementGameObject(value,wrapper);

				}
			}
			
			
			
			else
			{
				Debug.Log("inputobject is a object");
				//because this is the top level, we wont reflect over this object
				//but instead just generate an element that represents it as the root.
				
				if (subTreeRoot is IDynamicMetaObjectProvider)
				{
					Debug.Log("inputobject is a dynamic object");
					var names = new List<string>();
					var dynobj = subTreeRoot as IronPython.Runtime.Binding.IPythonExpandable;
					if (dynobj != null)
					{
						
						names.AddRange(dynobj.Context.LanguageContext.GetMemberNames(Expression.Constant(dynobj)));
					}
					
					//filter names so that python private and builtin members do not show
					var filterednames = names.Where(x => x.StartsWith("__") != true).ToList();
					
					foreach (var name in filterednames)
					{
						
						var value = InspectorVisualization.GetDynamicValue(dynobj, name);
						
						if (value != null)
						{
							 InspectorVisualization.generateInspectableElementGameObject(value,wrapper);

							
						}
						
						
					}
					
				}
				
				// if object was not dynamic use regular reflection
				else
				{
					Debug.Log("inputobject is a non dynamic object");
					var propertyInfos = subTreeRoot.GetType().GetProperties(
						BindingFlags.Public | BindingFlags.NonPublic // Get public and non-public
						| BindingFlags.Static | BindingFlags.Instance  // Get instance + static
						| BindingFlags.FlattenHierarchy); // Search up the hierarchy
					
					
					
					foreach (var prop in propertyInfos.ToList())
					{
						if (prop.GetIndexParameters().Length > 0)
						{
							// Property is an indexer
							Debug.Log("this property is an indexed property, for now we won't reflect further");
							continue;
						}

						var value = prop.GetValue(subTreeRoot, null);
						InspectorVisualization.generateInspectableElementGameObject(value,wrapper,prop.Name);

						
					}
				}
				
				
				
			}



		}

	}
}