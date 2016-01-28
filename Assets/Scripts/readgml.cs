using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using Random = UnityEngine.Random;

public class readgml : MonoBehaviour {

	public struct EDGE {
		public int target, source;
	}

	public struct NODE {
		public int id;
		public string label;
		public float x_pos, y_pos, z_pos;
		public float temp_x, temp_y, temp_z;
		public float force;
		public float offset_x, offset_y, offset_z;
		public List<EDGE> edgeList;
	}

	public class GRAPH {
		public int n_nodes = 0;
		public int n_edges = 0;
		public Dictionary<int, NODE> nodes = new Dictionary<int, NODE>();
		public List<EDGE> edges = new List<EDGE>();
	}

    public GRAPH graph = new GRAPH();

	// Use this for initialization
	void Start () {
		//fill_buffer
		//create_network
		//get_degrees
		//red_edges
		//free_buffer

		read_file("dolphins.gml");

		//graph complete
		ForceDirected.InitGraph (graph);
		ForceDirected.GenerateGraph ();
		//ForceDirected f = new ForceDirected ();
		//f.DrawGraph ();

		GraphController.setGraph (graph);
	
		bool result = true; //printGraph();

        if (result)
            print("IT'S FIXED!!");
        else
            print("Try again :(");
	}

	public int read_file(string filename)
	{
		string line;

		System.IO.StreamReader file = 
			new System.IO.StreamReader(filename);

		bool newNode = false;
		int lastID = -1;
		int lastSource = -1;
		bool newEdge = false;
		float area = 25.0f;

        while ((line = file.ReadLine()) != null)
		{
			char[] delims = {' '};
			string[] words = line.Split(delims);

            int i = 0;
            while (i < words.Length)
            {
                if (words[i].CompareTo("") == 0)
                {
                    i++;
                    continue;
                }

                else if (words[i].CompareTo("id") == 0)
                {
                    newNode = true;

                    // add new node to graph, set id as words[i+1]
                    lastID = Convert.ToInt32(words[i + 1]);
                    break;
                }

                else if (words[i].CompareTo("label") == 0)
                {
                    if (newNode)
                    {
                        // get the last node created, add a label
                        // ignore quotations
                        NODE n = new NODE();
                        n.id = lastID;
                        n.label = words[i + 1];

						n.x_pos = Mathf.Floor (Random.Range(-area, area));
						n.y_pos = Mathf.Floor (Random.Range(-area, area));
						n.z_pos = Mathf.Floor (Random.Range(-area, area));

						n.temp_x = -50.0f;
						n.temp_y = -50.0f;
						n.temp_z = -50.0f;

						n.edgeList = new List<EDGE> ();

                        graph.nodes.Add(n.id, n);
                        graph.n_nodes++;

                        newNode = false;
                        lastID = -1;
                        break;
                    }
                    else {
                        // unknown id read, no label accompanying
                        break;
                    }
                }

                else if (words[i].CompareTo("source") == 0)
                {
                    newEdge = true;

                    // add new edge from indexes
                    lastSource = Convert.ToInt32(words[i + 1]);
                    break;
                }

                else if (words[i].CompareTo("target") == 0)
                {
                    if (newEdge)
                    {
                        // get last edge value created
                        EDGE e = new EDGE();
                        e.source = lastSource;
                        e.target = Convert.ToInt32(words[i + 1]);
                        graph.edges.Insert(graph.n_edges, e);
                        graph.n_edges++;

						graph.nodes [e.source].edgeList.Add (e);
						graph.nodes [e.target].edgeList.Add (e);

                        newEdge = false;
                        lastSource = -1;
                        break;
                    }
                    else {
                        // failure
                        break;
                    }
                }

                else // ignore any other line, namely "[" and "]"
                {
                    break;
                }
            }
		}

        file.Close();
		return 1;
	}

	public bool printGraph() {
		if(graph.n_nodes <= 0 || graph.n_edges <= 0) {
			return false;
		}
		else {
			for(int i = 0; i < graph.n_nodes; i++) {
				print("Node " + i + ": label = " + graph.nodes[i].label + " (" + graph.nodes[i].x_pos + ", " + graph.nodes[i].y_pos +
					", " + graph.nodes[i].z_pos + ")");
			}
			for(int x = 0; x < graph.n_edges; x++) {
				print("Edge " + x + ": source = " + graph.edges[x].source + ", target = " + graph.edges[x].target);
			}
		}
		 
		return true;
	}
}