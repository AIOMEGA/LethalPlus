using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using UnityEngine;
using LethalPlus;
using BepInEx.Logging;
using UnityEditor;
using HarmonyLib;
using GameNetcodeStuff;
using System.Collections;
using Unity.Netcode;
using UnityEngine.AI;
using System.Runtime.CompilerServices;
using LethalLib.Modules;
using LC_API;

namespace LethalPlus
{
    internal class PitFiendAI : EnemyAI
    {
        public float detectionRadius = 12f;

        private Collider[] allPlayerColliders = (Collider[])(object)new Collider[4];

        private float closestPlayerDist;

        private Collider tempTargetCollider;

        public bool detectingPlayers;

        

        public override void Start()
        {
            base.Start();
            movingTowardsTargetPlayer = true;
            Main.Log.LogInfo("PitFiend Loaded");
            Main.Log.LogInfo("Mod Loaded");

        }

        public override void DoAIInterval()
        {
            int num = Physics.OverlapSphereNonAlloc(base.transform.position, detectionRadius, allPlayerColliders, StartOfRound.Instance.playersMask);
            if (num > 0)
            {
                detectingPlayers = true;
                closestPlayerDist = 255555f;
                for (int i = 0; i < num; i++)
                {
                    float num2 = Vector3.Distance(base.transform.position, ((Component)(object)allPlayerColliders[i]).transform.position);
                    if (num2 < closestPlayerDist)
                    {
                        closestPlayerDist = num2;
                        tempTargetCollider = allPlayerColliders[i];
                    }
                }

                SetMovingTowardsTargetPlayer(((Component)(object)tempTargetCollider).gameObject.GetComponent<PlayerControllerB>());
            }
            else
            {
                this.agent.speed = 5f;
                detectingPlayers = false;
            }

            base.DoAIInterval();
        }

        public override void Update()
        {
            if (base.IsOwner && detectingPlayers)
            {
                this.agent.speed = Mathf.Clamp(this.agent.speed + Time.deltaTime / 3f, 0f, 12f);
            }

            base.Update();
        }

        protected override void __initializeVariables()
        {
            base.__initializeVariables();
        }

        protected override string __getTypeName()
        {
            return "PitFiendAI";
        }
    }
}
