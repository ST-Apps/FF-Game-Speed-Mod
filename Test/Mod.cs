using MelonLoader;
using UnityEngine;

namespace Test
{
    public class Mod : MelonMod
    {
        public override void OnUpdate()
        {
            //This is one of Melon API function we can use to add features : https://melonwiki.xyz/#/modders/quickstart
            //Here only a key to exit the game and another to spawn a villager on cursor position
            if (Input.GetKeyDown(KeyCode.T))
            {
                Application.Quit();
            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                MelonLogger.Msg("V pressed");
                GameManager gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
                if (gameManager != null)
                {
                    Vector3 mousePosition = Input.mousePosition;
                    Vector3 terrainWorldPointUnderScreenPoint = gameManager.terrainManager.GetTerrainWorldPointUnderScreenPoint(mousePosition);
                    gameManager.villagerPopulationManager.SpawnVillagerImmigration(terrainWorldPointUnderScreenPoint, true);
                }
            }
        }
    }
}