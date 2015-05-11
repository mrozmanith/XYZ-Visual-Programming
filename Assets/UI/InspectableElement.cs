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
using System.ComponentModel;

namespace Nodeplay.UI
{
	/// <summary>
	/// TODO may design this class as generic so we can keep track of the type
	/// and have it work with a monobehavior based class that points here
	/// </summary>
	[RequireComponent(typeof(LayoutElement))]
	[RequireComponent(typeof(EventConsumer))]
	//[RequireComponent(typeof(HorizontalLayoutGroup))]
	public class InspectableElement: UIBehaviour ,IPointerClickHandler,INotifyPropertyChanged
	{
		public NodeModel Model;
		public Type ElementType;
		public object Reference;
		private bool exposesubElements = false;
		public string Name;
		private GameObject gridprefab = Resources.Load<GameObject>("Grid");
		private Material linematerial = Resources.Load<Material> ("LineMat");
		// Use this for initialization


		private Vector3 location;
		public Vector3 Location
			
		{
			get
			{
				return this.location;
				
			}
			
			set
			{
				if (value != this.location)
				{
					this.location = value;
					NotifyPropertyChanged("Location");

				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void NotifyPropertyChanged(String info)
		{
			//Debug.Log("sending " + info + " change notification");
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}

		private void handleRectChanges(object sender, PropertyChangedEventArgs info)
		{
			if (info.PropertyName == "Location"){
			//	cleanupVisualization();
			//	UpdateVisualization();
			}
		}

		private void cleanupVisualization ()
		{
			var childrenofVisualization = GetComponentInChildren<InspectableElement>().transform.Cast<Transform>().ToList();

			childrenofVisualization.AddRange(transform.FindChild("visualizationParent").transform.Cast<Transform>().ToList());
			if (childrenofVisualization.Count > 0){
				childrenofVisualization.Where(x=>x.CompareTag("visualization")).ToList().ForEach(x=>DestroyImmediate(x.gameObject));
			}

		}

		private static void SortChildrenByBaseTypeName(GameObject parent) 
		{
				List<Transform> children = new List<Transform>();
			//iterate children of this transform backwards and remove all children
				for (int i = parent.transform.childCount - 1; i >= 0; i--) {
					Transform child = parent.transform.GetChild(i);
					children.Add(child);
				//remove child from parent 
					child.SetParent(null,false);
				}
			//now sort each child using the name of type it represents then by value... but this doesnt preserve the original order......
			children.OrderBy(x=>x.GetComponentInChildren<InspectableElement>().Reference.GetType().Name).ThenBy(y=>y.GetComponent<InspectableElement>().Reference);
				
			//put all the children back under their parent
			foreach (Transform child in children) 
				{
					child.SetParent(parent.transform,false);
				}
			}
		
		protected override void Start()
		{
			Model = this.transform.root.GetComponentInChildren<NodeModel>();

			this.PropertyChanged += handleRectChanges;

		}
		protected virtual void Update()
		{
			if (transform.hasChanged) {
				Location = this.gameObject.transform.position;
				transform.hasChanged = false;
			}

			
		}
		
		public void UpdateVisualization(){


			var visualization = searchforvisualization(this.Reference);
			visualization.tag = "visualization";

			//find the visualization parent
			var fontsize = GetComponentInChildren<Text>().fontSize;
			var depth = (int)(500/fontsize);

			var viszparent = this.transform.FindChild("visualizationParent");
			visualization.transform.SetParent(viszparent.transform,false);
			visualization.transform.localScale = visualization.transform.localScale * (1000/depth);

			if (visualization.GetComponentInChildren<Renderer>()!=null)
			{
				var allrenderers = visualization.GetComponentsInChildren<Renderer>().ToList();
				var totalBounds = allrenderers[0].bounds;
				foreach (Renderer ren in allrenderers)
				{
					totalBounds.Encapsulate(ren.bounds);	
				}
				var boundingBox = totalBounds;

			viszparent.GetComponent<LayoutElement>().preferredWidth = boundingBox.size.x * (4000)*2;
				viszparent.GetComponent<LayoutElement>().preferredHeight = boundingBox.size.y* (4000)*2;
			}
			visualization.AddComponent<Button>().onClick.AddListener(()=>{toggleWorldSpaceVisualization(this.Reference);});

			//now calculate the 
		}

		private Vector3 calculateCentroid (List<Vector3>points)
		{
			Vector3 center = Vector3.zero;
			foreach (var point in points)
			{
				center = center + point;
			}
			center = center / (points.Count);
			return center;
		}

		//also need to destroy whatever is created by searching for visualization...
		//should parent this as before so that it's cleaned up correctly
		private void toggleWorldSpaceVisualization(object objectToDrawLineTo)
			{

				if(transform.FindChild("viz line") == null)
				{
						var visualizations = new List<GameObject>();
						var visualization = searchforvisualization(objectToDrawLineTo,VisualizationContext.Worldspace);
						
						//if the visualization returned is a proper visualization
						// then just draw a line to it
						if (!visualization.CompareTag("visualization"))
						    {

						foreach (Transform child in visualization.GetComponentsInChildren<Transform>())
						{
							child.parent = null;
							if (child.gameObject.CompareTag("visualization"))
							{
								visualizations.Add(child.gameObject);
							}
							else{
								Destroy(child.gameObject);
							}
						}
						Destroy(visualization);
						}
						else
						{
							visualizations.Add(visualization);
						}

						drawlineToVisualization(visualizations.First().transform.position);
						visualizations.First().transform.SetParent(this.transform);

				}
				else{
				GameObject.Destroy(transform.FindChild("viz line").gameObject);
					}
			}


		private void drawlineToVisualization(Vector3 To)
		{

			Debug.Log("current drawing a line that represents:" + this.gameObject.name + Name);

			var line = new GameObject("viz line");
			line.transform.SetParent(this.transform);
			line.tag = "visualization";
			line.AddComponent<LineRenderer>();
			var linerenderer = line.GetComponent<LineRenderer>();
			linerenderer.useWorldSpace = true;
			linerenderer.SetVertexCount(2);

			var from = this.GetComponentInChildren<Text>().transform.position;
			linerenderer.SetPosition(0,from);
			linerenderer.SetPosition(1,To);
			linerenderer.material = linematerial;
			linerenderer.SetWidth(.05f,.05f);
		}

		enum VisualizationContext
		{
			List,
			Worldspace
		};
		//tentatively return a new gameobject that renders some representation of this object
		//probably based on its type, so we'll need a mapping from type to visualization
		private GameObject searchforvisualization(object objectToVisualize, VisualizationContext context = VisualizationContext.List)
		{
			//if a gameobject then just copy the gameobject which will use its
			//renderer if it has one
			if (objectToVisualize is UnityEngine.GameObject)
			{

				if (((GameObject)objectToVisualize).GetComponent<Renderer>() != null)
					{
					//if this object is a line renderer we need to set the worldspace property to false before returning
					if (((GameObject)objectToVisualize).GetComponent<LineRenderer>() != null)
					{
						var localSpaceLine = Instantiate(((GameObject)objectToVisualize)) as GameObject;
						localSpaceLine.GetComponent<LineRenderer>().useWorldSpace = false;
						localSpaceLine.tag = "visualization";
						return localSpaceLine;
					}

					//just return the actual object and we'll just move it.
					 var taggedGO = Instantiate(((GameObject)objectToVisualize)) as GameObject; 
						taggedGO.tag = "visualization"; 
					return taggedGO;
						
					}

				else{
					//this is a unityobject with no renderer, potentially many things that would be good to visualize
					//like colliders, images,sprites,text...prefabs,meshes,etc,etc
					return (new GameObject("unimplemented visualization"));
					}

			}

			else
			{
				//this is any other type not a unity object:
				//if a vector2 or 3

				if (objectToVisualize is Vector2 || objectToVisualize is Vector3)
				{
					//first load the grid object

					var grid = GameObject.Instantiate(gridprefab);
					grid.transform.localScale = new Vector3(1,1,1);
					//then place a point into the grid
					//grid.tag = "listonlyVisualization";
					//if we get a point or vector, lets visualize it as a point,
					//insert it as a child of the grid

					var point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
					point.transform.parent = grid.transform;
					//if we're generating a list element visualization then clamp the scale...
					if (context == VisualizationContext.List)
					{
						point.transform.localPosition = Vector3.ClampMagnitude(((Vector3)objectToVisualize),3);
					} 
					else
					{
						point.transform.localPosition = ((Vector3)objectToVisualize);
					}

					point.tag = "visualization";
					return grid;
				}


				//if a transform

				//if a list

				//if a dictionary

				//if a number

				//if a string

				//
				return (new GameObject("unimplemented visualization"));

			}

		
		}


		public void UpdateText(object pointer = null)
		{
			var fontsize = GetComponentInChildren<Text>().fontSize;
			var extraText = "";
			if (InspectorVisualization.IsList(Reference))
			{
				extraText = string.Join(string.Empty,Enumerable.Repeat(" ..[.].. ",(Reference as IList).Count).ToArray());
			}

			if (pointer == null)
			{

				GetComponentInChildren<Text>().text = "<color=teal>"+ElementType.ToString()+"</color>"  + 
					" : \n " +"<color=orange>"+Name+"</color>"+
					": \n " + "<size="+(fontsize*1.5).ToString()+">"+Reference.ToString()+extraText+"</size>"; 
			}
			else
			{

				GetComponentInChildren<Text>().text = "<color=teal>"+pointer.GetType().ToString()+"</color>" +
					": \n "+ "<size="+(fontsize*1.5).ToString()+">"+ pointer.ToString()+extraText+"</size>"; 
			}
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (exposesubElements == false){
				exposesubElements = true;
				var wrapper = populateNextLevel(this.Reference);
				this.GetComponentsInChildren<Image>().ToList().ForEach(x=>x.color = Color.green);
				SortChildrenByBaseTypeName(wrapper);

			}
			else{

				var childrenRoot = transform.parent.GetChild(1);
				GameObject.DestroyImmediate(childrenRoot.gameObject);
				exposesubElements = false;
				this.GetComponentsInChildren<Image>().ToList().ForEach(x=>x.color = Color.white);
			}
		}


		private GameObject populateNextLevel(System.Object subTreeRoot)
		{
			//build a new wrapper for this next level
			var wrapper = new GameObject("sub_tree_wrapper");
			wrapper.tag = "visualization";
			wrapper.transform.SetParent(this.transform.parent,false);
			wrapper.AddComponent<HorizontalLayoutGroup>();
			wrapper.GetComponent<HorizontalLayoutGroup>().spacing = 5;
			

			if (InspectorVisualization.IsList(subTreeRoot))
			{
//				Debug.Log("inputobject is a list");
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
						try
						{
						var value = prop.GetValue(subTreeRoot, null);
						InspectorVisualization.generateInspectableElementGameObject(value,wrapper,prop.Name);
						}
						catch(Exception e)
						{
							Debug.Log("could not get property" + prop.Name);
						}
						
					}
				}
				
				
				
			}

			return wrapper;

		}

	}
}
