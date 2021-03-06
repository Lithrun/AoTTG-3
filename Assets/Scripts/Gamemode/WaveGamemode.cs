﻿using Assets.Scripts.Characters.Titan;
using Assets.Scripts.Characters.Titan.Behavior;
using Assets.Scripts.Gamemode.Settings;
using Assets.Scripts.UI.Input;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Gamemode
{
    public class WaveGamemode : GamemodeBase
    {
        private int highestWave = 1;

        public bool PunkWave { get; set; } = true;
        private readonly int _punkWave = 5;
        public int Wave = 1;

        public sealed override GamemodeSettings Settings { get; set; }
        private WaveGamemodeSettings GamemodeSettings => Settings as WaveGamemodeSettings;

        public override string GetGamemodeStatusTop(int totalRoomTime = 0, int timeLeft = 0)
        {
            var content = "Titan Left: ";
            object[] objArray = new object[4];
            objArray[0] = content;
            var length = GameObject.FindGameObjectsWithTag("titan").Length;
            objArray[1] = length.ToString();
            objArray[2] = " Wave : ";
            objArray[3] = Wave;
            return string.Concat(objArray);
        }

        public override string GetGamemodeStatusTopRight(int time = 0, int totalRoomTime = 0)
        {
            if (PhotonNetwork.offlineMode)
            {
                var content = "Time : ";
                var length = totalRoomTime;
                return content + length.ToString();
            }
            return base.GetGamemodeStatusTopRight(time, totalRoomTime);
        }

        public override string GetVictoryMessage(float timeUntilRestart, float totalServerTime = 0f)
        {
            if (PhotonNetwork.offlineMode)
            {
                return $"Survive All Waves!\n Press {InputManager.GetKey(InputUi.Restart)} to Restart.\n\n\n";
            }
            return $"Survive All Waves!\nGame Restart in {(int) timeUntilRestart}s\n\n";
        }

        public override string GetDefeatMessage(float gameEndCd)
        {
            if (PhotonNetwork.offlineMode)
            {
                return $"Survive {Wave} Waves!\n Press {InputManager.GetKey(InputUi.Restart)} to Restart.\n\n\n";
            }
            return $"Survive {Wave} Waves!\nGame Restart in {(int) gameEndCd}s\n\n";
        }

        public override string GetRoundEndedMessage()
        {
            return $"Highest Wave : {highestWave}";
        }

        public override void OnLevelLoaded(Level level, bool isMasterClient = false)
        {
            base.OnLevelLoaded(level, isMasterClient);
            if (!isMasterClient) return;
            if (GamemodeSettings.Name.Contains("Annie"))
            {
                PhotonNetwork.Instantiate("FEMALE_TITAN", GameObject.Find("titanRespawn").transform.position, GameObject.Find("titanRespawn").transform.rotation, 0);
            }
            else
            {
                StartCoroutine(SpawnTitan(GamemodeSettings.Titans));
            }
        }

        public override void OnRestart()
        {
            Wave = GamemodeSettings.StartWave;
            base.OnRestart();
        }

        private TitanConfiguration GetWaveTitanConfiguration()
        {
            var configuration = GetTitanConfiguration();
            configuration.Behaviors.Add(new WaveBehavior());
            configuration.ViewDistance = 999999f;
            return configuration;
        }

        private TitanConfiguration GetWaveTitanConfiguration(MindlessTitanType type)
        {
            var configuration = GetTitanConfiguration(type);
            configuration.Behaviors.Add(new WaveBehavior());
            configuration.ViewDistance = 999999f;
            return configuration;
        }

        public override void OnTitanKilled(string titanName)
        {
            if (!IsAllTitansDead()) return;
            Wave++;
            var level = FengGameManagerMKII.Level.Name;
            if (!(GamemodeSettings.RespawnMode != RespawnMode.NEWROUND && (!level.StartsWith("Custom"))))
            {
                foreach (var player in PhotonNetwork.playerList)
                {
                    if (RCextensions.returnIntFromObject(player.CustomProperties[PhotonPlayerProperty.isTitan]) != 2)
                    {
                        FengGameManagerMKII.instance.photonView.RPC("respawnHeroInNewRound", player);
                    }
                }
            }
            //if (IN_GAME_MAIN_CAMERA.gametype == GAMETYPE.MULTIPLAYER)
            //{
            //    //this.sendChatContentInfo("<color=#A8FF24>Wave : " + this.wave + "</color>");
            //}
            if (Wave > highestWave)
            {
                highestWave = Wave;
            }
            if (PhotonNetwork.isMasterClient)
            {
                FengGameManagerMKII.instance.RequireStatus();
            }
            if (!((GamemodeSettings.MaxWave != 0 || Wave <= GamemodeSettings.MaxWave) && (GamemodeSettings.MaxWave <= 0 || Wave <= GamemodeSettings.MaxWave)))
            {
                FengGameManagerMKII.instance.gameWin2();
            }
            else
            {
                if (Wave % _punkWave == 0)
                {
                    for (int i = 0; i < Wave / _punkWave; i++)
                    {
                        FengGameManagerMKII.instance.SpawnTitan(GetWaveTitanConfiguration(MindlessTitanType.Punk));
                    }
                }
                else
                {
                    StartCoroutine(SpawnTitan(GamemodeSettings.Titans + Wave * GamemodeSettings.WaveIncrement));
                }
            }
        }

        IEnumerator SpawnTitan(int titans)
        {
            var spawns = GameObject.FindGameObjectsWithTag("titanRespawn");
            for (int i = 0; i < titans; i++)
            {
                var randomSpawn = spawns[Random.Range(0, spawns.Length)];
                FengGameManagerMKII.instance.SpawnTitan(randomSpawn.transform.position, randomSpawn.transform.rotation, GetWaveTitanConfiguration());
                yield return new WaitForEndOfFrame();
            }
        }
    }
}
