﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;



public class SpawnManager : MonoBehaviour {

	// The default player prefab.
	public GameObject playerPrefab;

	private GameObject[] spawnPoints;
	private float spawnRadius = 5f;
	private float radiusCheck = 1.5f;
	private int maxSpawnAttempts = 50;
	private List<NetworkPlayer> playerList = new List<NetworkPlayer>();
	private List<NetworkPlayer> readyList = new List<NetworkPlayer>();

	// Use this for initialization
	void Start () {
		if (Network.isClient) 
		{
			networkView.RPC("ClientReady", RPCMode.Server, Network.player);
		}
		if (Network.isServer && Network.connections.Length == 0)
		{
			ChooseRole();
		}
	}

	void ChooseRole()
	{
		if(Network.isServer){
			playerList.Add (Network.player);
			for (int i = 0; i < Network.connections.Length; i++) 
			{
				playerList.Add (Network.connections[i]);
			}
			Debug.Log(playerList.Count);
			int monsterIndex = Random.Range (0, playerList.Count);
			Debug.Log ("Creating Monster...");
			if (playerList [monsterIndex] == Network.player) 
			{
				SpawnPlayer("Monster");
			}
			else
			{
				networkView.RPC("SpawnPlayer", playerList[monsterIndex], "Monster");
			}
			playerList.RemoveAt(monsterIndex);
			
			if (playerList.Count >= 6) 
			{
				int cultistIndex = Random.Range(0,playerList.Count);
				Debug.Log ("Creating Cultist...");
				if (playerList [cultistIndex] == Network.player) 
				{
					SpawnPlayer("Cultist");
				}
				else
				{
					networkView.RPC("SpawnPlayer", playerList[cultistIndex], "Cultist");
				}
				playerList.RemoveAt(cultistIndex);
			}
			
			int numOfRoles = Mathf.FloorToInt(playerList.Count / 2);
			while (playerList.Count < numOfRoles) 
			{
				Debug.Log ("Generating Special Roles...");
				int specialIndex = Random.Range(0,playerList.Count);
				int role = Random.Range (0,100);
				if(role < 50)
				{
					Debug.Log ("Creating Priest...");
					if (playerList [specialIndex] == Network.player) 
					{
						SpawnPlayer("Priest");
					}
					else
					{
						networkView.RPC("SpawnPlayer", playerList[specialIndex], "Priest");
					}
				}
				if(role >= 50)
				{
					Debug.Log ("Creating Assassin...");
					if (playerList [specialIndex] == Network.player) 
					{
						SpawnPlayer("Assassin");
					}
					else
					{
						networkView.RPC("SpawnPlayer", playerList[specialIndex], "Assassin");
					}
				}
				playerList.RemoveAt(specialIndex);
			}
			while (playerList.Count > 0) 
			{
				int normIndex = Random.Range (0,playerList.Count);
				int normRole = Random.Range (0,100);
				if(normRole < 50)
				{
					Debug.Log ("Creating Peasant...");
					if (playerList [normIndex] == Network.player) 
					{
						SpawnPlayer("Peasant");
					}
					else
					{
						networkView.RPC("SpawnPlayer", playerList[normIndex], "Peasant");
					}
				}
				if(normRole >= 50)
				{
					Debug.Log ("Creating Survivor...");
					if (playerList [normIndex] == Network.player) 
					{
						SpawnPlayer("Survivor");
					}
					else
					{
						networkView.RPC("SpawnPlayer", playerList[normIndex], "Survivor");
					}
				}
				playerList.RemoveAt(normIndex);
			}
		}
	}

	[RPC] void ClientReady(NetworkPlayer player)
	{
		readyList.Add(player);
		if (readyList.Count == playerList.Count) 
		{
			ChooseRole();
		}
	}



	// Create a player object.
	[RPC] void SpawnPlayer(string role)
	{
		if (role == "Monster") 
		{
			spawnPoints = GameObject.FindGameObjectsWithTag("MonsterSpawn");
			int index = Random.Range(0, spawnPoints.Length);
			Vector3 spawnPoint = spawnPoints[index].transform.position;
			Vector3 spawn = new Vector3(spawnPoint.x + Random.Range(-spawnRadius, spawnRadius), spawnPoint.y, spawnPoint.z + Random.Range(-spawnRadius, spawnRadius));
			for(int i = 0; i < maxSpawnAttempts; i++){
				if(Physics.CheckSphere(spawn,radiusCheck))
				{
					spawn = new Vector3(spawnPoint.x + Random.Range(-spawnRadius, spawnRadius), spawnPoint.y, spawnPoint.z + Random.Range(-spawnRadius, spawnRadius));
				}
				else
				{
					break;
				}
			}
			Debug.Log("Spawning Player");
			GameObject player = (GameObject)Network.Instantiate(playerPrefab, spawn, Quaternion.identity, 0);
			player.transform.FindChild ("Menu").FindChild ("RoleNamePanel").FindChild ("RoleName").GetComponent<Text>().text = "Monster";
			player.transform.FindChild ("Menu").FindChild ("RoleDescriptionPanel").FindChild ("RoleDescription").GetComponent<Text>().text = "You're a Monster";
		}
		else
		{
			spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");
			int index = Random.Range(0, spawnPoints.Length);
			Vector3 spawnPoint = spawnPoints[index].transform.position;
			Vector3 spawn = new Vector3(spawnPoint.x + Random.Range(-spawnRadius, spawnRadius), spawnPoint.y, spawnPoint.z + Random.Range(-spawnRadius, spawnRadius));
			for(int i = 0; i < maxSpawnAttempts; i++){
				if(Physics.CheckSphere(spawn,radiusCheck))
				{
					spawn = new Vector3(spawnPoint.x + Random.Range(-spawnRadius, spawnRadius), spawnPoint.y, spawnPoint.z + Random.Range(-spawnRadius, spawnRadius));
				}
				else
				{
					break;
				}
			}
			Debug.Log("Spawning Player");
			GameObject player = (GameObject)Network.Instantiate(playerPrefab, spawn, Quaternion.identity, 0);
			switch (role) 
			{
			case "Cultist":
				player.transform.FindChild ("Menu").FindChild ("RoleNamePanel").FindChild ("RoleName").GetComponent<Text>().text = "Cultist";
				player.transform.FindChild ("Menu").FindChild ("RoleDescriptionPanel").FindChild ("RoleDescription").GetComponent<Text>().text = "You're a Cultist";
				break;
			case "Peasant":
				player.transform.FindChild ("Menu").FindChild ("RoleNamePanel").FindChild ("RoleName").GetComponent<Text>().text = "Peasant";
				player.transform.FindChild ("Menu").FindChild ("RoleDescriptionPanel").FindChild ("RoleDescription").GetComponent<Text>().text = "You're a Peasant";
				break;
			case "Survivor":
				player.transform.FindChild ("Menu").FindChild ("RoleNamePanel").FindChild ("RoleName").GetComponent<Text>().text = "Survivor";
				player.transform.FindChild ("Menu").FindChild ("RoleDescriptionPanel").FindChild ("RoleDescription").GetComponent<Text>().text = "You're a Survivor";
				break;
			case "Priest":
				player.transform.FindChild ("Menu").FindChild ("RoleNamePanel").FindChild ("RoleName").GetComponent<Text>().text = "Priest";
				player.transform.FindChild ("Menu").FindChild ("RoleDescriptionPanel").FindChild ("RoleDescription").GetComponent<Text>().text = "You're a Priest";
				break;
			case "Assassin":
				player.transform.FindChild ("Menu").FindChild ("RoleNamePanel").FindChild ("RoleName").GetComponent<Text>().text = "Assassin";
				player.transform.FindChild ("Menu").FindChild ("RoleDescriptionPanel").FindChild ("RoleDescription").GetComponent<Text>().text = "You're a Assassin";
				break;
			}
		}
	}
}	