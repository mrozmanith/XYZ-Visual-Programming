﻿using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Nodeplay.Interfaces;
using System.ComponentModel;

using IronPython;
using IronPython.Modules;
using System.Text;
using Microsoft.Scripting.Hosting;
using System;
using System.IO;
using Nodeplay.Engine;

namespace Nodeplay.Engine
{
    class PythonEvaluator : Evaluator
    {
		
        public String StdOut;
        private ScriptScope scope;

        public void Start()
        {
            //names = new List<String>() { "name1", "name2" };
            //vals = new List<System.Object>() { 1, 2 };
            //outnames = {range}
            //Debug.Log(Evaluate(code, names, vals));

        }

        public override Dictionary<string,object> Evaluate(EvaluationPackage evalpackage)
        {

            var engine = IronPython.Hosting.Python.CreateEngine();

             scope = engine.CreateScope();
			//http://techartsurvival.blogspot.com/2013/12/techartists-doin-it-for-themselves.html#gpluscomments
			engine.Runtime.LoadAssembly (typeof(PythonIOModule).Assembly);  
			engine.Runtime.LoadAssembly (typeof(GameObject).Assembly);  
			engine.Runtime.LoadAssembly (typeof(Editor).Assembly);  
			string dllpath = System.IO.Path.GetDirectoryName (  
			                                                  (typeof(ScriptEngine)).Assembly.Location).Replace (  
			                                                   "\\", "/");  


			// load needed modules and paths  
			StringBuilder init = new StringBuilder ();  
			init.AppendLine ("import sys");  
			init.AppendFormat ("sys.path.append(\"{0}\")\n", dllpath + "/Lib");  
			init.AppendFormat ("sys.path.append(\"{0}\")\n", dllpath + "/DLLs");  
			init.AppendLine ("import UnityEngine as unity");  
			init.AppendLine ("import UnityEditor as editor");  
			init.AppendLine ("import StringIO");  
			init.AppendLine ("unity.Debug.Log(\"Python console initialized\")");  


			var OutputNames = evalpackage.OutputNames;
			var variableNames = evalpackage.VariableNames;
			var variableValues = evalpackage.VariableValues;
			var ExecutionPointers = evalpackage.ExecutionPointers;
			var script = evalpackage.Code;

            foreach (var variable in variableNames)
            {
                var index = variableNames.IndexOf(variable);
                // do we need to do some conversion of this type...TODO
                scope.SetVariable(variable, variableValues[index]);
                Debug.Log("setting" + variable + "to" + variableValues[index].ToString());
            }

			foreach (var pointer in ExecutionPointers)
			{

				scope.SetVariable(pointer.First, pointer.Second);
				Debug.Log("setting " + pointer.First + " to " + pointer.Second.ToString() + " in python context");
				//TODO if this list is empty or if the script never exposes these triggers then we need
				//complain at compile time, throw an exception, or just inject one at the end of the 
				//script or after this eval returns
			}


			using (var memoryStream = new MemoryStream())
			{
                engine.Runtime.IO.SetOutput(memoryStream, new StreamWriter(memoryStream));
                try
                {

                    engine.CreateScriptSourceFromString(init.ToString()+script).Execute(scope);
                }
                catch (Exception e)
                {

                    string error = engine.GetService<ExceptionOperations>().FormatException(e);
                    Debug.LogException(e);
                }
                finally
                {

                    var length = (int)memoryStream.Length;
                    var bytes = new byte[length];
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    memoryStream.Read(bytes, 0, length);
                    StdOut = Encoding.UTF8.GetString(bytes, 0, length).Trim();
					Debug.Log("<color=yellow>Python Std:</color>" + StdOut);

                }
                //TODO we need a way to push values from the python context to the nodeModel
				//during execution, not sure if this is possible, for example, to gather
				//some intermediate outputs during a for loop execution, possible implementation
				//can just inject some logic into the callOutput method, that gathers all output
				//variables, if they are not set or cannot be found, just continue execution
				// but don't fail, they just might not be set yet


                var outdict = PollScopeForOutputs(OutputNames);
                
               

                return outdict;
            }


        }

        public override  Dictionary<string, object> PollScopeForOutputs(List<string> OutputNames)
        {
            var outdict = new Dictionary<string, object>();

            foreach (var outname in OutputNames)
            {
                if (scope.ContainsVariable(outname))
                {
                    outdict[outname] = scope.GetVariable(outname);
                }
                else
                {
                    outdict[outname] = "No variable named" + outname + "was defined in the python code";
                }
            }
            return outdict;
        }

    }
}
		 

