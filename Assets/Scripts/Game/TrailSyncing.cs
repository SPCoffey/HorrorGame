﻿using UnityEngine;
using System.Collections;

public class TrailSyncing : MonoBehaviour {

	public int trailTime;
	public float trailGreen;
	public float trailRed;
	public GameObject player;

	//Syncs up the trailtime between players and makes the trailrenderer time = the trailtime
	private void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
	{
		if (stream.isWriting) 
		{
			stream.Serialize(ref trailTime);
			stream.Serialize(ref trailGreen);
			stream.Serialize(ref trailRed);
		}
		else
		{
			stream.Serialize(ref trailTime);
			stream.Serialize(ref trailGreen);
			stream.Serialize(ref trailRed);
			player.transform.FindChild("TrailRenderer").GetComponent<TrailRenderer>().time = trailTime;
			player.transform.FindChild("TrailRenderer").GetComponent<TrailRenderer>().material.SetColor("_TintColor", new Color(trailRed,trailGreen, 0, 1));
		}
	}

	//The equation for the trailtime based on health
	public void SyncTrails(int health)
	{
		trailTime = 15 + (30 / ((health/10) + 1));
		trailRed = 1f/(((float)health / 10f) + 1f);
		trailGreen = ((float)health/100f);
	}
}