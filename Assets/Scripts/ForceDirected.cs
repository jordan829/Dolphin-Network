using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ForceDirected : MonoBehaviour {

	public static float attraction_multiplier;
	public static float repulsion_multiplier;
	public static int max_iterations;
	public static readgml.GRAPH graph;
	public static float width;
	public static float height;
	public static bool finished;
	public static float epsilon;
	public static float attraction_constant;
	public static float repulsion_constant;
	public static float force_constant;
	public static int layout_iterations;
	public static float temperature;
	public static int nodes_length;
	public static int edges_length;

	public Transform Node_fab;
	public bool drawn = false;
	public static List<Transform> gObjects = new List<Transform>();

	void Start()
	{
	}

	public static void InitGraph(readgml.GRAPH g)
	{
		attraction_multiplier = 5;
		repulsion_multiplier = 0.75f;
		max_iterations = 1000;
		width = 200;
		height = 200;
		finished = false;
		epsilon = .000001f;
		layout_iterations = 0;
		temperature = width / 10.0f;
		graph = g;
		nodes_length = graph.n_nodes;
		edges_length = graph.n_edges;
		force_constant = Mathf.Sqrt (height * width / nodes_length);
		attraction_constant = attraction_multiplier * force_constant;
		repulsion_constant = repulsion_multiplier * force_constant;
	}

	public static bool GenerateGraph()
	{
		if(layout_iterations < max_iterations && temperature > 0.000001) {

			// Calculate Repulsion
			for(int i = 0; i < nodes_length; i++) {
				readgml.NODE node_v = graph.nodes [i];
				if (i == 0) {
					node_v.offset_x = 0;
					node_v.offset_y = 0;
					node_v.offset_z = 0;
				}
				node_v.force = 0;
				node_v.temp_x = (node_v.temp_x != -50.0f) ? node_v.temp_x : node_v.x_pos;
				node_v.temp_y = (node_v.temp_y != -50.0f) ? node_v.temp_y : node_v.y_pos;
				node_v.temp_z = (node_v.temp_z != -50.0f) ? node_v.temp_z : node_v.z_pos;


				for(int j = i+1; j < nodes_length; j++) {
					readgml.NODE node_u = graph.nodes [j];
					if (i != j) {
						node_u.temp_x = (node_u.temp_x != -50) ? node_u.temp_x : node_u.x_pos;
						node_u.temp_y = (node_u.temp_y != -50) ? node_u.temp_y : node_u.y_pos;
						node_u.temp_z = (node_u.temp_z != -50) ? node_u.temp_z : node_u.z_pos;

						float delta_x = node_v.temp_x - node_u.temp_x;
						float delta_y = node_v.temp_y - node_u.temp_y;
						float delta_z = node_v.temp_z - node_u.temp_z;

						float delta_length = Mathf.Max (epsilon, Mathf.Sqrt ((delta_x * delta_x) + (delta_y * delta_y) + (delta_z * delta_z)));

						float force = (repulsion_constant * repulsion_constant) / delta_length;

						node_v.force += force;
						node_u.force += force;

						// update node_v's offsets
						node_v.offset_x += (delta_x / delta_length) * force;
						node_v.offset_y += (delta_y / delta_length) * force;
						node_v.offset_z += (delta_z / delta_length) * force;

						if(i == 0) {
							node_u.offset_x = 0;
							node_u.offset_y = 0;
							node_u.offset_z = 0;
						}
						node_u.offset_x -= (delta_x / delta_length) * force;
						node_u.offset_y -= (delta_x / delta_length) * force;
						node_u.offset_z -= (delta_z / delta_length) * force;
					}
					graph.nodes [j] = node_u;
				}
				graph.nodes [i] = node_v;
			}


			// Calculate Attraction
			for(int i = 0; i < edges_length; i++) {
				readgml.EDGE edge = graph.edges [i];
				readgml.NODE node_source = graph.nodes [edge.source];
				readgml.NODE node_target = graph.nodes [edge.target];

				float delta_x = node_source.temp_x - node_target.temp_x;
				float delta_y = node_source.temp_y - node_target.temp_y;
				float delta_z = node_source.temp_z - node_target.temp_z;

				float delta_length = Mathf.Max (epsilon, Mathf.Sqrt ((delta_x * delta_x) + (delta_y * delta_y) + (delta_z * delta_z)));

				float force = (delta_length * delta_length) / attraction_constant;

				node_source.force -= force;
				node_target.force += force;

				node_source.offset_x -= (delta_x / delta_length) * force;
				node_source.offset_y -= (delta_y / delta_length) * force;
				node_source.offset_z -= (delta_z / delta_length) * force;

				node_target.offset_x += (delta_x / delta_length) * force;
				node_target.offset_y += (delta_y / delta_length) * force;
				node_target.offset_z += (delta_z / delta_length) * force;
				graph.nodes [edge.source] = node_source;
				graph.nodes [edge.target] = node_target;
			}


			// Calculate Final Positions
			for(int i = 0; i < nodes_length; i++) {
				readgml.NODE node = graph.nodes [i];
				float delta_length = Mathf.Max (epsilon, Mathf.Sqrt (node.offset_x * node.offset_x + node.offset_y * node.offset_y + node.offset_z * node.offset_z));

				node.temp_x += (node.offset_x / delta_length) * Mathf.Min (delta_length, temperature);
				node.temp_y += (node.offset_y / delta_length) * Mathf.Min (delta_length, temperature);
				node.temp_z += (node.offset_z / delta_length) * Mathf.Min (delta_length, temperature);

				node.x_pos -= (node.x_pos - node.temp_x) / 10;
				node.y_pos -= (node.y_pos - node.temp_y) / 10;
				node.z_pos -= (node.z_pos - node.temp_z) / 10;

				graph.nodes [i] = node;
				//print ("(" + node.x_pos + ", " + node.y_pos + ", " + node.z_pos + ")");
			}

			temperature *= (1 - (layout_iterations/ max_iterations));
			layout_iterations++;
		}
		else {
			if(!finished) {
				
			}
			finished = true;
			return false;
		}

		return true;
	} // end of GenerateGraph


	public static void StopCalculating() {
		layout_iterations = max_iterations;
	}

	public void DrawGraph()
	{
		if(drawn) {
			/* print (gObjects.Count);
			while( gObjects.Count > 0 ) {
				Destroy (gObjects[0]);
				gObjects.RemoveAt (0);
			}
			drawn = false; */
            return;
		}

		for (int i = 0; i < nodes_length; i++) {
			readgml.NODE n = graph.nodes [i];
            Transform node = ((Transform)Instantiate(Node_fab, new Vector3(n.x_pos, n.y_pos, n.z_pos), Quaternion.identity));
			gObjects.Add (node);
            node.parent = transform;
			//Node.transform.parent = transform;
			//Node_fab.SetParent (transform, true);
		}

		drawn = true;
	}

	public void Update()
	{
		DrawGraph();
	}

	// end of ForceDirected.cs
}
