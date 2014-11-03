﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Nodeplay.Interfaces;
using System.ComponentModel;



    class PortView:BaseView<PortModel>
    {

        protected override void Start()
        {
            base.Start();
            PositionNewPort(this.gameObject);
        }

        /// <summary>
        /// a tempconnector that is drawn while dragging.
        /// </summary>
        private GameObject tempconnector;

        //TODO this method should be on portview
        public GameObject PositionNewPort(GameObject port)
        {
           
            //bb of go

            //might need to be owner model - this might work for now as NodeView and Model should be the common base classes
            var view = this.Model.Owner.GetComponent<NodeView>();
            var boundingBox = view.UI.renderer.bounds;
            port.transform.parent = Model.Owner.transform;
            port.transform.position = Model.Owner.transform.position;
            // move the port from the center to back or front depending on port type

            float direction;

            if (port.GetComponent<PortModel>().PortType == PortModel.porttype.input)
            {
                direction = -1f;
            }
            else
            {
                direction = 1f;
            }

            port.transform.Translate(0, 0, boundingBox.size.z * direction);
            port.transform.localScale = new Vector3(.33f, .33f, .33f);

            // now we need to move the port in relation up or down to all other ports,
            // and possibly adjust other ports as well

            List<PortModel> ports;

            if (port.GetComponent<PortModel>().PortType == PortModel.porttype.input)
            {
                ports = Model.Owner.Inputs;
            }
            else
            {
                ports = Model.Owner.Outputs;
            }


            foreach (var currentport in ports)
            {
                var index = ports.IndexOf(currentport);
                Debug.Log(boundingBox.size.y);
                Debug.Log(index);
                Debug.Log(ports.Count);
                currentport.gameObject.transform.localPosition = new Vector3(currentport.gameObject.transform.localPosition.x,
                    ((boundingBox.size.y * 1.5f) * ((float)index / (float)ports.Count)) * 2 - boundingBox.size.y * 1.5f,
                currentport.gameObject.transform.localPosition.z);
            }
            return port;
        }
        //event handlers
        // TODO THESE ARE CURRENTLY THE SAME AS NODEMODEL - need to be changed
        // and in some cases they should fire new events, like connection and unconnection
        // these events/handlers logic need to be worked through.
        // ports will be responsible for creating connectors

        public override GuiState MyOnMouseUp(GuiState current_state)
        {


            //handle this here for now:
            //destruction of temp connector
            if (tempconnector != null)
            {
                DestroyImmediate(tempconnector);
                tempconnector = null;
            }

            //if we mouseUp over a port we need to check if we were connecting/dragging,
            // and then we'll instantiate a new connectorModel, the model will create it's own view
            // and the view will listen to its ports for property changes
            GuiState newState;
            Debug.Log("Mouse up event handler called");

            //appears current_state is does not have correct MOUSEPOS... what else is incorrect...
            // this bug was fixed by not adding all new generated states to the state stream, only those coming from elements being interacted with
            if (HitTest(this.gameObject, current_state))
            {
                //if we were connecting then we can create a connector model
                if ((current_state.Connecting))
                {
                    // TODO REFACTOR THIS AS A METHOD TO KEEP MOUSEUP CLEAN
                    Debug.Log("I" + Model.NickName + " was just MouseUpedOn");

                    newState = new GuiState(true, false, current_state.MousePos, new List<GameObject>(), false);
                    var startport = current_state.Selection[0].GetComponent<PortModel>();
                    //TODO we need some logic to check if the port we just tried to connect to was an input or an output - 
                    // and if the port we are coming from is an input or an output
                    // Outputs - > Inputs
                    // Inputs - > Outputs
                    if (startport.PortType == Model.PortType)
                    {
                        newState = new GuiState(false, false, current_state.MousePos, new List<GameObject>(), false);
                        GuiTest.statelist.Add(newState);
                        return newState;
                    }

                    //TODO we must also look if we're about to create a cyclic dependencey, we should return a blank state

                    // if port is already connected then disconnect old port before creating new connector
                    if (Model.IsConnected)
                    {	//TODO THIS ONLY MAKES SENSE FOR INPUT NODES... REDESIGN
                        Model.Disconnect(Model.connectors[0]);
                        //TODO //probably also need to discconnect the other port as well :( and the connector or another manager should
                        //probably take care of sending this event chains

                    }
                    //instantiate new connector etc
                    //TODO move this method to portmodel?
                    // or possibly all instantiation to a manager or WorldModel/Controller
                    var realConnector = new GameObject("Connector");
                    realConnector.AddComponent<ConnectorModel>();
                    realConnector.GetComponent<ConnectorModel>().init(current_state.Selection[0].GetComponent<PortModel>(), Model);
                    Model.Connect(realConnector.GetComponent<ConnectorModel>());

                    //TODO the other port also needs a connect signal
                    current_state.Selection[0].GetComponent<PortModel>().Connect(realConnector.GetComponent<ConnectorModel>());


                }
                else
                {
                    // if we were not connecting then gen a blank state, just a mouse up, and clear selection
                    newState = new GuiState(false, false, current_state.MousePos, new List<GameObject>(), false);
                }
                GuiTest.statelist.Add(newState);
                return newState;
            }
            else
            {
                // if we mouseupped over nothing
                return null;

            }


        }

        //handler for dragging node event//
        //TODO there is a bug when dragging from an input to an output which destroys the other connectors from that output
        //fixes might include restricting connecting creation only if the we drag on outputs, and mouseup on inputs
        // or investingating why this is not working further, connector model is being destroyed but still refereneced
        public override GuiState MyOnMouseDrag(GuiState current_state)
        {
            GuiState newState = current_state;
            Debug.Log("drag event handler, on a port");
            //TODO need to enable dragging of old connections away and disconnection of connectors
            if (current_state.Selection.Contains(this.gameObject) && this.GetComponent<PortModel>().PortType == PortModel.porttype.output)
            {   // If doing a mouse drag with this component selected...
                // since we are in 3d space now, we need to conver this to a vector3...
                // for now just use the z coordinate of the first object
                // project from camera through mouse currently and use same distance
                Vector3 to_point = ProjectCurrentDrag(dist_to_camera);


                if (tempconnector != null)
                {
                    DestroyImmediate(tempconnector);
                    tempconnector = null;
                }
                // since this is a port, we need to instantiate a new 
                //ConnectorView ( this is a temporary connector that we drag around in the UI)

                tempconnector = new GameObject("ConnectorViewTemp");
                tempconnector.AddComponent<ConnectorView>();
                tempconnector.GetComponent<ConnectorView>().init(this.gameObject.transform.position, to_point);

                // move object to new coordinate
                newState = new GuiState(true, true, Input.mousePosition, current_state.Selection, false);
                GuiTest.statelist.Add(newState);
                Event.current.Use();

            }
            return newState;


        }
        //handler for clicks
        public override GuiState MyOnMouseDown(GuiState current_state)
        {
            Debug.Log("mouse down event handler called");
            // check if this node was actually clicked on
            if (HitTest(this.gameObject, current_state))
            {
                Debug.Log("I" + this.name + " was just clicked");
                dist_to_camera = Vector3.Distance(this.transform.position, Camera.main.transform.position);

                Debug.Log(current_state);

                // check the dragstate from the GUI, either this is a double click
                // or a selection click
                // or possibly a click on nothing
                if (current_state.DoubleClicked == false)
                {

                    // add this item to the current selection
                    // update the drag state
                    // store this drag state in the list of all dragstates

                    // eventually we'll need to check if this port is an input or output

                    List<GameObject> new_sel = (new List<GameObject>(current_state.Selection));
                    new_sel.Add(this.gameObject);
                    var newState = new GuiState(current_state.Connecting, current_state.Dragging, current_state.MousePos, new_sel, false);
                    GuiTest.statelist.Add(newState);
                    GeneratedDragState = newState;


                }
                else
                {
                    //a double click occured on a node
                    Debug.Log("I" + this.name + " was just DOUBLE clicked");

                }

                // finally return the new dragstate(not what this function should return)
                // since we actually have the dragstate stored
                // it might make more sense not to allow the Node to 
                // store the state on the GUI...
                return GeneratedDragState;



            }
            else
            {
                return null;
            }
        }

        public override void onGuiRepaint()
        {

        }


    }
