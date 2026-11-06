#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Worms.LevelValidation
{
    /// <summary>Explicit creation of minimal physical fixtures, never a production-level migration.</summary>
    public static class FixtureAuthoring
    {
        public const string Folder = "Assets/Tests/LevelValidation/Fixtures/";
        public static void Create()
        {
            if (EditorSceneManager.GetActiveScene().isDirty) throw new System.InvalidOperationException("Save or discard the current dirty scene before creating fixtures.");
            Directory.CreateDirectory(Folder);
            if (!File.Exists(Folder + "EmptyReset.unity"))
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), Folder + "EmptyReset.unity");
            }
            foreach (string name in new[] { "StaticRoute", "SealedExit", "MovingHazard", "BreakableWheel", "NarrowExit", "DirectExit" })
            {
                if (File.Exists(Folder + name + ".unity")) continue;
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var playerObject = new GameObject("Player");
                playerObject.tag = "Player";
                playerObject.transform.position = new Vector3(0, 3);
                var body = playerObject.AddComponent<Rigidbody2D>();
                body.gravityScale = 0;
                playerObject.AddComponent<CircleCollider2D>().radius = .15f;
                playerObject.AddComponent<Player>();
                playerObject.AddComponent<Explode>();
                var oxygen = new GameObject("Oxygen").AddComponent<Oxygen>();
                oxygen.air = oxygen.maxAir = 6;
                var wheelObject = new GameObject("Wheel");
                wheelObject.tag = "Wheel";
                wheelObject.AddComponent<CircleCollider2D>().radius = 1;
                var wheel = wheelObject.AddComponent<Wheel>();
                wheel.speed = 90;
                wheel.direction = -1;
                wheel.holder = new GameObject("Holder").transform;
                wheel.holder.SetParent(wheelObject.transform, false);
                wheelObject.AddComponent<WheelSurface>().wheel = wheel;
                if (name == "BreakableWheel") wheelObject.AddComponent<CountdownExplode>().timer = 1.5f;
                var finish = new GameObject("Finish");
                finish.transform.position = name == "DirectExit" ? new Vector3(0, 1.7f) : new Vector3(4, 0);
                var trigger = finish.AddComponent<BoxCollider2D>();
                trigger.isTrigger = true;
                trigger.size = name == "NarrowExit" ? new Vector2(.2f, .05f) : new Vector2(.8f, 1.5f);
                finish.AddComponent<Finish>();
                finish.AddComponent<SaveGame>();
                if (name == "SealedExit")
                {
                    Wall(new Vector2(3, 0), new Vector2(.6f, 4));
                    Wall(new Vector2(5, 0), new Vector2(.6f, 4));
                    Wall(new Vector2(4, 1.7f), new Vector2(2.6f, .6f));
                    Wall(new Vector2(4, -1.7f), new Vector2(2.6f, .6f));
                }
                if (name == "MovingHazard")
                {
                    var hazard = new GameObject("Hazard");
                    hazard.tag = "Deadly";
                    hazard.AddComponent<BoxCollider2D>().size = new Vector2(.3f, .8f);
                    var moving = hazard.AddComponent<MovingSpikes>();
                    moving.startPos = new GameObject("Start").transform;
                    moving.endPos = new GameObject("End").transform;
                    moving.startPos.position = new Vector3(2.5f, -2);
                    moving.endPos.position = new Vector3(2.5f, 2);
                    moving.speed = .5f;
                }
                EditorSceneManager.SaveScene(scene, Folder + name + ".unity");
            }
            AssetDatabase.Refresh();
            foreach (string name in new[] { "StaticRoute", "SealedExit", "MovingHazard", "BreakableWheel", "NarrowExit", "DirectExit" })
            {
                var manifest = SceneInspection.Inspect(Folder + name + ".unity")[0];
                File.WriteAllText(Folder + name + ".json", JsonUtility.ToJson(manifest, true));
            }
            AssetDatabase.Refresh();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        private static void Wall(Vector2 position, Vector2 size)
        {
            var wall = new GameObject("Wall");
            wall.transform.position = position;
            wall.AddComponent<BoxCollider2D>().size = size;
        }
    }
}
#endif
