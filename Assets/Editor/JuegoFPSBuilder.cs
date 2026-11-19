using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JuegoFPS.Editor
{
    [InitializeOnLoad]
    public static class JuegoFPSBuilder
    {
        private const string ScenesDir = "Assets/Scenes";
        private const string PrefabsDir = "Assets/GameAssets/Prefabs";
        private const string MaterialsDir = "Assets/GameAssets/Materials";
        private const int BuilderVersion = 11;

        static JuegoFPSBuilder()
        {
            EditorApplication.delayCall += AutoBuildIfNeeded;
        }

        private static void AutoBuildIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            string level1Path = $"{ScenesDir}/{GameManager.SceneLevel1}.unity";
            if (!File.Exists(level1Path) || EditorPrefs.GetInt("JuegoFPS_BuilderVersion", 0) < BuilderVersion)
            {
                EditorPrefs.SetInt("JuegoFPS_BuilderVersion", BuilderVersion);
                BuildAll();
            }
        }

        [MenuItem("JuegoFPS/Construir Proyecto Completo (Escenas y Prefabs)")]
        public static void BuildAll()
        {
            EnsureFolders();

            // 1. Materiales URP
            Material matFloor = CreateLitMaterial("Mat_Floor", new Color(0.18f, 0.2f, 0.22f), 0.1f, 0.35f);
            Material matWall = CreateLitMaterial("Mat_Wall", new Color(0.28f, 0.3f, 0.33f), 0.0f, 0.25f);
            Material matCover = CreateLitMaterial("Mat_Cover", new Color(0.45f, 0.33f, 0.22f), 0.0f, 0.2f);
            Material matDianaNormal = CreateLitMaterial("Mat_DianaNormal", new Color(0.9f, 0.22f, 0.18f), 0.1f, 0.4f);
            Material matDianaOro = CreateLitMaterial("Mat_DianaOro", new Color(1f, 0.78f, 0.15f), 0.85f, 0.75f);
            Material matDianaMovil = CreateLitMaterial("Mat_DianaMovil", new Color(0.15f, 0.82f, 0.95f), 0.3f, 0.6f);
            Material matDianaMaestra = CreateLitMaterial("Mat_DianaMaestra", new Color(0.75f, 0.2f, 0.95f), 0.5f, 0.7f);
            Material matSkeleton = CreateSkeletonMaterial("Mat_Skeleton", false);
            Material matBoss = CreateSkeletonMaterial("Mat_Boss", true);

            // 2. Prefabs
            GameObject playerPrefab = CreatePlayerPrefab();
            GameObject skeletonPrefab = CreateEnemyPrefab("Enemy_Skeleton", false, matSkeleton);
            GameObject bossPrefab = CreateEnemyPrefab("Boss_Skeleton", true, matBoss);

            // 3. Escenas
            string menuPath = BuildMainMenuScene();
            string level1Path = BuildLevel1Scene(playerPrefab, skeletonPrefab, matFloor, matWall, matDianaNormal, matDianaOro, matDianaMovil, matDianaMaestra);
            string level2Path = BuildLevel2Scene(playerPrefab, skeletonPrefab, matFloor, matWall, matCover);
            string level3Path = BuildLevel3Scene(playerPrefab, skeletonPrefab, bossPrefab, matFloor, matWall, matCover);

            // 4. Registrar en Build Settings
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(menuPath, true),
                new EditorBuildSettingsScene(level1Path, true),
                new EditorBuildSettingsScene(level2Path, true),
                new EditorBuildSettingsScene(level3Path, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Abrir la escena del Menú Principal
            EditorSceneManager.OpenScene(menuPath);
            Debug.Log("[JuegoFPSBuilder] Proyecto construido correctamente: 4 escenas, prefabs y Build Settings actualizados.");
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(ScenesDir);
            Directory.CreateDirectory(PrefabsDir);
            Directory.CreateDirectory(MaterialsDir);
        }

        private static Material CreateLitMaterial(string name, Color color, float metallic, float smoothness)
        {
            string path = $"{MaterialsDir}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");

            if (mat == null)
            {
                mat = new Material(urpLit);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = urpLit;
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;

            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateSkeletonMaterial(string name, bool isBoss)
        {
            string path = $"{MaterialsDir}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");

            if (mat == null)
            {
                mat = new Material(urpLit);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = urpLit;
            }

            string albedoPath = isBoss
                ? "Assets/GameAssets/Textures/Skeleton/T_Skeleton_Variant_1_D_Albedo.png"
                : "Assets/GameAssets/Textures/Skeleton/T_Skeleton_Variant_1_A_Albedo.png";

            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GameAssets/Textures/Skeleton/T_Skeleton_Variant_1_N_OpenGL.png");
            Texture2D emissive = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GameAssets/Textures/Skeleton/T_Skeleton_Variant_1_Emissive.png");

            if (albedo != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", albedo);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", isBoss ? new Color(1f, 0.65f, 0.75f) : Color.white);
            if (normal != null && mat.HasProperty("_BumpMap")) mat.SetTexture("_BumpMap", normal);

            if (isBoss && emissive != null && mat.HasProperty("_EmissionMap"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetTexture("_EmissionMap", emissive);
                mat.SetColor("_EmissionColor", new Color(1.4f, 0.25f, 0.4f));
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static GameObject CreatePlayerPrefab()
        {
            GameObject root = new GameObject("PlayerFPS");
            root.tag = "Player";

            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            PlayerControllerFPS pc = root.AddComponent<PlayerControllerFPS>();
            WeaponController wc = root.AddComponent<WeaponController>();

            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.SetParent(root.transform, false);
            camObj.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            Camera cam = camObj.AddComponent<Camera>();
            cam.nearClipPlane = 0.08f;
            camObj.AddComponent<AudioListener>();

            pc.cameraTransform = camObj.transform;
            wc.playerCamera = cam;

            // Holder de armas
            GameObject holder = new GameObject("WeaponHolder");
            holder.transform.SetParent(camObj.transform, false);
            holder.transform.localPosition = new Vector3(0.26f, -0.24f, 0.52f);
            wc.weaponHolder = holder.transform;

            // Manos FPS (.glb) - Tecla 1
            GameObject handsPivot = new GameObject("HandsPivot");
            handsPivot.transform.SetParent(camObj.transform, false);
            handsPivot.transform.localPosition = new Vector3(0.14f, -0.62f, 0.28f);
            handsPivot.transform.localRotation = Quaternion.identity;

            GameObject handsAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameAssets/Models/Hands/free_fps_arms_with_glove.glb");
            if (handsAsset != null)
            {
                GameObject hands = Object.Instantiate(handsAsset, handsPivot.transform);
                hands.name = "FPS_Hands";
                NormalizeModelSize(hands, 1.35f, true);
                RemoveAllColliders(hands);
            }
            handsPivot.SetActive(false);
            wc.handsModel = handsPivot;

            // Pistola M1911 (envuelta en Pivot para garantizar rotación correcta) - Tecla 2
            GameObject pistolPivot = new GameObject("PistolPivot");
            pistolPivot.transform.SetParent(holder.transform, false);
            pistolPivot.transform.localPosition = Vector3.zero;
            pistolPivot.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            GameObject m1911Asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameAssets/Models/Weapons/M1911.fbx");
            GameObject pistolObj = m1911Asset != null
                ? Object.Instantiate(m1911Asset, pistolPivot.transform)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);
            pistolObj.name = "M1911";
            if (m1911Asset != null)
            {
                NormalizeModelSize(pistolObj, 0.35f, false);
            }
            else
            {
                pistolObj.transform.localScale = new Vector3(0.06f, 0.12f, 0.28f);
            }
            RemoveAllColliders(pistolObj);
            wc.pistolModel = pistolPivot;

            // Rifle SCAR (envuelto en Pivot, apunta hacia +Z con rotación 0) - Tecla 3
            GameObject riflePivot = new GameObject("RiflePivot");
            riflePivot.transform.SetParent(holder.transform, false);
            riflePivot.transform.localPosition = new Vector3(0f, -0.02f, 0.08f);
            riflePivot.transform.localRotation = Quaternion.identity;

            GameObject scarAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameAssets/Models/Weapons/SCAR.fbx");
            GameObject rifleObj = scarAsset != null
                ? Object.Instantiate(scarAsset, riflePivot.transform)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);
            rifleObj.name = "SCAR";
            if (scarAsset != null)
            {
                NormalizeModelSize(rifleObj, 0.65f, false);
            }
            else
            {
                rifleObj.transform.localScale = new Vector3(0.07f, 0.14f, 0.55f);
            }
            RemoveAllColliders(rifleObj);
            riflePivot.SetActive(false);
            wc.rifleModel = riflePivot;

            // Muzzle + Luz
            GameObject muzzle = new GameObject("MuzzlePoint");
            muzzle.transform.SetParent(holder.transform, false);
            muzzle.transform.localPosition = new Vector3(0f, 0.04f, 0.42f);
            wc.muzzlePoint = muzzle.transform;

            Light flash = muzzle.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.color = new Color(1f, 0.8f, 0.35f);
            flash.intensity = 4f;
            flash.range = 6f;
            flash.enabled = false;
            wc.muzzleFlashLight = flash;

            string prefabPath = $"{PrefabsDir}/PlayerFPS.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateEnemyPrefab(string prefabName, bool isBoss, Material mat)
        {
            GameObject root = new GameObject(prefabName);

            GameObject skelAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameAssets/Models/Enemies/SM_Skeleton_Variant_1.fbx");
            if (skelAsset == null)
            {
                skelAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameAssets/Models/Enemies/SKM_Skeleton_Var_1.fbx");
            }

            float targetHeight = isBoss ? 3.5f : 1.9f;

            GameObject visual;
            if (skelAsset != null)
            {
                visual = Object.Instantiate(skelAsset, root.transform);
                NormalizeModelSize(visual, targetHeight, true);
                ApplyMaterialRecursive(visual, mat);
                RemoveAllColliders(visual);
            }
            else
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visual.transform.SetParent(root.transform, false);
                visual.transform.localScale = isBoss ? new Vector3(1.4f, 1.75f, 1.4f) : new Vector3(0.8f, 0.95f, 0.8f);
                visual.transform.localPosition = new Vector3(0f, targetHeight * 0.5f, 0f);
                ApplyMaterialRecursive(visual, mat);
                RemoveAllColliders(visual);
            }

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.height = targetHeight;
            col.radius = isBoss ? 0.95f : 0.45f;
            col.center = new Vector3(0f, targetHeight * 0.5f, 0f);

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            EnemyController ec = root.AddComponent<EnemyController>();
            ec.isBoss = isBoss;
            ec.enemyName = isBoss ? "Señor Esqueleto (BOSS)" : "Esqueleto";
            ec.maxHealth = isBoss ? 250 : 30;
            ec.pointsValue = isBoss ? 500 : 150;
            ec.moveSpeed = isBoss ? 3.8f : 3.2f;
            ec.contactDamageDistance = isBoss ? 2.1f : 1.35f;

            string prefabPath = $"{PrefabsDir}/{prefabName}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static string BuildMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateGameManager();
            CreateEventSystem();

            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.1f, 0.14f);
            camObj.AddComponent<AudioListener>();

            Canvas canvas = CreateCanvas("MenuCanvas");
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Fondo sutil
            GameObject bg = CreateUIPanel("PanelFondo", canvas.transform, new Color(0.05f, 0.07f, 0.11f, 0.9f), Vector2.zero, Vector2.one);

            CreateUIText("Titulo", bg.transform, "OPERACIÓN: GALERÍA FPS", 48, FontStyle.Bold, Color.white,
                new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.78f), new Vector2(900, 90));

            CreateUIText("Subtitulo", bg.transform, "Proyecto Unity FPS - 3 Niveles (Galería, Enemigos y Boss)", 22, FontStyle.Normal, new Color(0.75f, 0.85f, 1f),
                new Vector2(0.5f, 0.69f), new Vector2(0.5f, 0.69f), new Vector2(800, 45));

            // Panel Principal: solo JUGAR y SALIR
            GameObject mainPanel = CreateUIPanel("PanelPrincipal", bg.transform, new Color(0f, 0f, 0f, 0f), Vector2.zero, Vector2.one);
            Button playBtn = CreateUIButton("BtnJugar", mainPanel.transform, "JUGAR", new Color(0.18f, 0.65f, 0.32f),
                new Vector2(0.5f, 0.48f), new Vector2(300, 66), font);
            Button quitBtn = CreateUIButton("BtnSalir", mainPanel.transform, "SALIR", new Color(0.68f, 0.22f, 0.22f),
                new Vector2(0.5f, 0.34f), new Vector2(300, 60), font);

            // Panel Selección de Niveles: Nivel 1, Nivel 2, Nivel 3 y Volver
            GameObject levelSelectPanel = CreateUIPanel("PanelNiveles", bg.transform, new Color(0f, 0f, 0f, 0f), Vector2.zero, Vector2.one);
            Button lvl1Btn = CreateUIButton("BtnNivel1", levelSelectPanel.transform, "NIVEL 1: GALERÍA DE DISPAROS", new Color(0.20f, 0.55f, 0.85f),
                new Vector2(0.5f, 0.54f), new Vector2(440, 60), font);
            Button lvl2Btn = CreateUIButton("BtnNivel2", levelSelectPanel.transform, "NIVEL 2: ENEMIGOS", new Color(0.82f, 0.52f, 0.16f),
                new Vector2(0.5f, 0.42f), new Vector2(440, 60), font);
            Button lvl3Btn = CreateUIButton("BtnNivel3", levelSelectPanel.transform, "NIVEL 3: BOSS FINAL", new Color(0.70f, 0.20f, 0.62f),
                new Vector2(0.5f, 0.30f), new Vector2(440, 60), font);
            Button backBtn = CreateUIButton("BtnVolver", levelSelectPanel.transform, "VOLVER", new Color(0.35f, 0.38f, 0.42f),
                new Vector2(0.5f, 0.17f), new Vector2(240, 48), font);
            levelSelectPanel.SetActive(false);

            MainMenuController mmc = canvas.gameObject.AddComponent<MainMenuController>();
            mmc.mainPanel = mainPanel;
            mmc.playButton = playBtn;
            mmc.quitButton = quitBtn;
            mmc.levelSelectPanel = levelSelectPanel;
            mmc.level1Button = lvl1Btn;
            mmc.level2Button = lvl2Btn;
            mmc.level3Button = lvl3Btn;
            mmc.backButton = backBtn;

            string path = $"{ScenesDir}/{GameManager.SceneMenu}.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static string BuildLevel1Scene(GameObject playerPrefab, GameObject skeletonPrefab,
            Material matFloor, Material matWall, Material matNormal, Material matOro, Material matMovil, Material matMaestra)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateGameManager();
            CreateEventSystem();
            CreateHUD();
            CreateDirectionalLight();

            // Suelo y paredes de la galería
            CreateArenaBounds(100f, 100f, matFloor, matWall);

            // Escenario decorativo shooting+env.FBX con escala realista y colisiones en todas sus paredes
            string envPath = "Assets/GameAssets/Models/Environment/shooting+env.FBX";
            ModelImporter envImporter = AssetImporter.GetAtPath(envPath) as ModelImporter;
            if (envImporter != null)
            {
                Directory.CreateDirectory("Assets/GameAssets/Textures/Environment");
                envImporter.ExtractTextures("Assets/GameAssets/Textures/Environment");
            }

            GameObject envAsset = AssetDatabase.LoadAssetAtPath<GameObject>(envPath);
            if (envAsset != null)
            {
                GameObject env = Object.Instantiate(envAsset);
                env.name = "Escenario_Galeria";
                NormalizeModelSize(env, 85f, true);
                env.transform.position = new Vector3(0f, 0.02f, 6f);
                AddMeshCollidersRecursive(env);
                ApplyEnvironmentMaterials(env);
            }

            // Jugador
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = new Vector3(-3f, 0.2f, -20f);

            // 10 Dianas diferenciadas
            CreateTarget("Diana_Normal_1", new Vector3(-6f, 1.8f, 2f), 50, false, matNormal);
            CreateTarget("Diana_Normal_2", new Vector3(-2f, 1.8f, 4f), 50, false, matNormal);
            CreateTarget("Diana_Normal_3", new Vector3(2f, 1.8f, 4f), 50, false, matNormal);
            CreateTarget("Diana_Normal_4", new Vector3(6f, 1.8f, 2f), 50, false, matNormal);

            CreateTarget("Diana_Oro_1", new Vector3(-5f, 3.0f, 9f), 100, false, matOro);
            CreateTarget("Diana_Oro_2", new Vector3(0f, 3.3f, 11f), 100, false, matOro);
            CreateTarget("Diana_Oro_3", new Vector3(5f, 3.0f, 9f), 100, false, matOro);

            CreateTarget("Diana_Movil_1", new Vector3(-4f, 2.2f, 14f), 150, true, matMovil, 3.5f, 2.2f);
            CreateTarget("Diana_Movil_2", new Vector3(4f, 2.6f, 16f), 150, true, matMovil, 3.5f, 2.6f);

            CreateTarget("Diana_Maestra", new Vector3(0f, 4.2f, 18f), 250, true, matMaestra, 5.0f, 3.2f);

            // 1 Esqueleto guardia opcional en lateral para probar colisión/vidas desde el nivel 1
            GameObject guard = (GameObject)PrefabUtility.InstantiatePrefab(skeletonPrefab);
            guard.name = "Esqueleto_Guardia";
            guard.transform.position = new Vector3(11f, 0f, 5f);
            EnemyController guardEc = guard.GetComponent<EnemyController>();
            guardEc.countsForLevelCompletion = false;
            guardEc.moveSpeed = 1.6f;
            guardEc.detectionRange = 14f;

            string path = $"{ScenesDir}/{GameManager.SceneLevel1}.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static string BuildLevel2Scene(GameObject playerPrefab, GameObject skeletonPrefab,
            Material matFloor, Material matWall, Material matCover)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateGameManager();
            CreateEventSystem();
            CreateHUD();
            CreateDirectionalLight();
            CreateArenaBounds(44f, 44f, matFloor, matWall);

            // Cajas de cobertura
            CreateCoverBox(new Vector3(-5f, 1f, -3f), new Vector3(2.4f, 2f, 1.2f), matCover);
            CreateCoverBox(new Vector3(5f, 1f, -3f), new Vector3(2.4f, 2f, 1.2f), matCover);
            CreateCoverBox(new Vector3(0f, 1f, 3f), new Vector3(3.2f, 2f, 1.4f), matCover);

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = new Vector3(0f, 0.1f, -15f);

            Vector3[] spawns =
            {
                new Vector3(-9f, 0f, 4f),
                new Vector3(9f, 0f, 4f),
                new Vector3(-6f, 0f, 11f),
                new Vector3(6f, 0f, 11f),
                new Vector3(-2f, 0f, 16f),
                new Vector3(2f, 0f, 16f)
            };

            for (int i = 0; i < spawns.Length; i++)
            {
                GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(skeletonPrefab);
                enemy.name = $"Esqueleto_{i + 1}";
                enemy.transform.position = spawns[i];
            }

            string path = $"{ScenesDir}/{GameManager.SceneLevel2}.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static string BuildLevel3Scene(GameObject playerPrefab, GameObject skeletonPrefab, GameObject bossPrefab,
            Material matFloor, Material matWall, Material matCover)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateGameManager();
            CreateEventSystem();
            CreateHUD();

            Light dirLight = CreateDirectionalLight();
            dirLight.color = new Color(1f, 0.85f, 0.8f);

            CreateArenaBounds(48f, 48f, matFloor, matWall);

            // Pilares de cobertura
            CreateCoverBox(new Vector3(-7f, 1.5f, -4f), new Vector3(2f, 3f, 2f), matCover);
            CreateCoverBox(new Vector3(7f, 1.5f, -4f), new Vector3(2f, 3f, 2f), matCover);
            CreateCoverBox(new Vector3(-7f, 1.5f, 6f), new Vector3(2f, 3f, 2f), matCover);
            CreateCoverBox(new Vector3(7f, 1.5f, 6f), new Vector3(2f, 3f, 2f), matCover);

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = new Vector3(0f, 0.1f, -17f);

            // Boss Final
            GameObject boss = (GameObject)PrefabUtility.InstantiatePrefab(bossPrefab);
            boss.transform.position = new Vector3(0f, 0f, 13f);

            // 2 Escoltas
            GameObject guard1 = (GameObject)PrefabUtility.InstantiatePrefab(skeletonPrefab);
            guard1.name = "Escolta_Boss_1";
            guard1.transform.position = new Vector3(-6f, 0f, 9f);

            GameObject guard2 = (GameObject)PrefabUtility.InstantiatePrefab(skeletonPrefab);
            guard2.name = "Escolta_Boss_2";
            guard2.transform.position = new Vector3(6f, 0f, 9f);

            string path = $"{ScenesDir}/{GameManager.SceneLevel3}.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static void CreateTarget(string name, Vector3 pos, int points, bool isMoving, Material mat, float moveDist = 2.5f, float moveSpeed = 2f)
        {
            GameObject targetAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameAssets/Models/Targets/target.glb");
            GameObject obj = new GameObject(name);

            // Raycast hacia abajo para apoyar la diana en el suelo o mostrador más cercano
            Vector3 finalPos = new Vector3(pos.x, 0f, pos.z);
            if (Physics.Raycast(new Vector3(pos.x, 8f, pos.z), Vector3.down, out RaycastHit hit, 20f))
            {
                finalPos.y = hit.point.y;
            }
            obj.transform.position = finalPos;

            GameObject visual;
            if (targetAsset != null)
            {
                visual = Object.Instantiate(targetAsset, obj.transform);
                visual.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f) * Quaternion.Euler(0f, 0f, 90f);
                NormalizeModelSize(visual, 1.9f, true);
                RemoveAllColliders(visual);
            }
            else
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                visual.transform.SetParent(obj.transform, false);
                visual.transform.localScale = new Vector3(1.5f, 0.12f, 1.5f);
                visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                visual.transform.localPosition = new Vector3(0f, 0.95f, 0f);
                RemoveAllColliders(visual);
            }

            ApplyMaterialRecursive(visual, mat);

            SphereCollider col = obj.AddComponent<SphereCollider>();
            col.center = new Vector3(0f, 1.2f, 0f);
            col.radius = 0.85f;

            TargetObject t = obj.AddComponent<TargetObject>();
            t.targetName = name.Replace("_", " ");
            t.pointsValue = points;
            t.isMoving = isMoving;
            t.moveDistance = moveDist;
            t.moveSpeed = moveSpeed;
            t.hitEffectColor = mat != null && mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.yellow;
        }

        private static void CreateArenaBounds(float width, float depth, Material matFloor, Material matWall)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Suelo";
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(width, 1f, depth);
            floor.GetComponent<Renderer>().sharedMaterial = matFloor;

            float wallHeight = 5f;
            CreateWall("Pared_Norte", new Vector3(0f, wallHeight * 0.5f, depth * 0.5f), new Vector3(width, wallHeight, 1f), matWall);
            CreateWall("Pared_Sur", new Vector3(0f, wallHeight * 0.5f, -depth * 0.5f), new Vector3(width, wallHeight, 1f), matWall);
            CreateWall("Pared_Este", new Vector3(width * 0.5f, wallHeight * 0.5f, 0f), new Vector3(1f, wallHeight, depth), matWall);
            CreateWall("Pared_Oeste", new Vector3(-width * 0.5f, wallHeight * 0.5f, 0f), new Vector3(1f, wallHeight, depth), matWall);
        }

        private static void CreateWall(string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void CreateCoverBox(Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Cobertura";
            box.transform.position = pos;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static Light CreateDirectionalLight()
        {
            GameObject lightObj = new GameObject("Directional Light");
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.25f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            return l;
        }

        private static void CreateGameManager()
        {
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
        }

        private static void CreateEventSystem()
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        private static void CreateHUD()
        {
            Canvas canvas = CreateCanvas("HUDCanvas");
            HUDManager hud = canvas.gameObject.AddComponent<HUDManager>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Mira central (Crosshair)
            GameObject chH = CreateUIPanel("CrosshairH", canvas.transform, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            RectTransform rtH = chH.GetComponent<RectTransform>();
            rtH.sizeDelta = new Vector2(18f, 2f);
            GameObject chV = CreateUIPanel("CrosshairV", canvas.transform, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            RectTransform rtV = chV.GetComponent<RectTransform>();
            rtV.sizeDelta = new Vector2(2f, 18f);

            // Textos HUD
            hud.scoreText = CreateUIText("ScoreText", canvas.transform, "PUNTUACIÓN: 0", 24, FontStyle.Bold, Color.yellow,
                new Vector2(0.02f, 0.94f), new Vector2(0.02f, 0.94f), new Vector2(360, 40), TextAnchor.MiddleLeft);

            hud.livesText = CreateUIText("LivesText", canvas.transform, "VIDAS: 3", 24, FontStyle.Bold, new Color(1f, 0.4f, 0.4f),
                new Vector2(0.02f, 0.88f), new Vector2(0.02f, 0.88f), new Vector2(300, 40), TextAnchor.MiddleLeft);

            hud.levelText = CreateUIText("LevelText", canvas.transform, "Nivel", 22, FontStyle.Bold, Color.white,
                new Vector2(0.5f, 0.95f), new Vector2(0.5f, 0.95f), new Vector2(700, 40));

            hud.feedbackText = CreateUIText("FeedbackText", canvas.transform, "", 24, FontStyle.Bold, new Color(0.4f, 1f, 0.5f),
                new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), new Vector2(700, 45));

            hud.ammoText = CreateUIText("AmmoText", canvas.transform, "MUNICIÓN: 18", 26, FontStyle.Bold, Color.cyan,
                new Vector2(0.98f, 0.08f), new Vector2(0.98f, 0.08f), new Vector2(340, 40), TextAnchor.MiddleRight);

            hud.weaponText = CreateUIText("WeaponText", canvas.transform, "ARMA: M1911 [1 / 2]", 18, FontStyle.Normal, Color.white,
                new Vector2(0.98f, 0.03f), new Vector2(0.98f, 0.03f), new Vector2(380, 35), TextAnchor.MiddleRight);

            // Barra de Boss
            GameObject bossPanel = CreateUIPanel("BossPanel", canvas.transform, new Color(0f, 0f, 0f, 0.65f),
                new Vector2(0.3f, 0.86f), new Vector2(0.7f, 0.92f));
            hud.bossPanel = bossPanel;
            hud.bossNameText = CreateUIText("BossName", bossPanel.transform, "BOSS", 18, FontStyle.Bold, new Color(1f, 0.4f, 0.6f),
                new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), new Vector2(400, 28));

            GameObject sliderObj = new GameObject("BossSlider");
            sliderObj.transform.SetParent(bossPanel.transform, false);
            RectTransform sliderRt = sliderObj.AddComponent<RectTransform>();
            sliderRt.anchorMin = new Vector2(0.05f, 0.15f);
            sliderRt.anchorMax = new Vector2(0.95f, 0.45f);
            sliderRt.offsetMin = Vector2.zero;
            sliderRt.offsetMax = Vector2.zero;

            Slider slider = sliderObj.AddComponent<Slider>();
            GameObject fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform faRt = fillArea.AddComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero;
            faRt.anchorMax = Vector2.one;
            faRt.offsetMin = Vector2.zero;
            faRt.offsetMax = Vector2.zero;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fRt = fill.AddComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = Vector2.one;
            fRt.offsetMin = Vector2.zero;
            fRt.offsetMax = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.9f, 0.15f, 0.25f);
            slider.fillRect = fRt;
            hud.bossHealthSlider = slider;
            bossPanel.SetActive(false);

            // Panel Final
            GameObject endPanel = CreateUIPanel("EndScreenPanel", canvas.transform, new Color(0.05f, 0.06f, 0.1f, 0.92f),
                Vector2.zero, Vector2.one);
            hud.endScreenPanel = endPanel;
            hud.endTitleText = CreateUIText("EndTitle", endPanel.transform, "FIN DEL JUEGO", 48, FontStyle.Bold, Color.white,
                new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), new Vector2(700, 80));
            hud.endDetailsText = CreateUIText("EndDetails", endPanel.transform, "", 24, FontStyle.Normal, Color.white,
                new Vector2(0.5f, 0.54f), new Vector2(0.5f, 0.54f), new Vector2(760, 150));
            hud.restartButton = CreateUIButton("BtnRestart", endPanel.transform, "VOLVER A JUGAR", new Color(0.18f, 0.65f, 0.32f),
                new Vector2(0.5f, 0.34f), new Vector2(300, 58), font);
            hud.menuButton = CreateUIButton("BtnMenu", endPanel.transform, "MENÚ PRINCIPAL", new Color(0.25f, 0.45f, 0.75f),
                new Vector2(0.5f, 0.22f), new Vector2(300, 54), font);
            endPanel.SetActive(false);
        }

        private static Canvas CreateCanvas(string name)
        {
            GameObject obj = new GameObject(name);
            Canvas c = obj.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler cs = obj.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            obj.AddComponent<GraphicRaycaster>();
            return c;
        }

        private static GameObject CreateUIPanel(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Image img = obj.AddComponent<Image>();
            img.color = color;
            return obj;
        }

        private static Text CreateUIText(string name, Transform parent, string text, int size, FontStyle style, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMin;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = sizeDelta;

            Text t = obj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = text;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = alignment;
            return t;
        }

        private static Button CreateUIButton(string name, Transform parent, string label, Color bgColor, Vector2 anchorPos, Vector2 sizeDelta, Font font)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorPos;
            rt.anchorMax = anchorPos;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = sizeDelta;

            Image img = obj.AddComponent<Image>();
            img.color = bgColor;

            Button btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;

            Text txt = CreateUIText("Text", obj.transform, label, 24, FontStyle.Bold, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), sizeDelta);
            txt.font = font;
            return btn;
        }

        private static void NormalizeModelSize(GameObject obj, float desiredMaxSize, bool placeOnGround)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float maxDim = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (maxDim > 0.0001f)
            {
                float scaleFactor = desiredMaxSize / maxDim;
                obj.transform.localScale *= scaleFactor;
            }

            if (placeOnGround)
            {
                renderers = obj.GetComponentsInChildren<Renderer>();
                Bounds newBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    newBounds.Encapsulate(renderers[i].bounds);
                }
                Vector3 parentPos = obj.transform.parent != null ? obj.transform.parent.position : Vector3.zero;
                obj.transform.position -= new Vector3(
                    newBounds.center.x - parentPos.x,
                    newBounds.min.y - parentPos.y,
                    newBounds.center.z - parentPos.z);
            }
        }

        private static void ApplyMaterialRecursive(GameObject obj, Material mat)
        {
            if (mat == null) return;
            foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
            {
                r.sharedMaterial = mat;
            }
        }

        private static void RemoveAllColliders(GameObject obj)
        {
            foreach (Collider c in obj.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(c);
            }
        }

        private static void AddMeshCollidersRecursive(GameObject obj)
        {
            foreach (MeshFilter mf in obj.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh != null && mf.gameObject.GetComponent<Collider>() == null)
                {
                    mf.gameObject.AddComponent<MeshCollider>();
                }
            }
        }

        private static void ApplyEnvironmentMaterials(GameObject env)
        {
            Texture2D wallTex = CreateProceduralTexture("Tex_EnvWall", new Color(0.32f, 0.35f, 0.39f), new Color(0.24f, 0.27f, 0.30f), false);
            Texture2D floorTex = CreateProceduralTexture("Tex_EnvFloor", new Color(0.22f, 0.24f, 0.27f), new Color(0.16f, 0.18f, 0.20f), true);
            Texture2D propTex = CreateProceduralTexture("Tex_EnvProp", new Color(0.45f, 0.34f, 0.22f), new Color(0.32f, 0.23f, 0.14f), true);

            Material matEnvWall = CreateTexturedLitMaterial("Mat_EnvWall", wallTex, 0.05f, 0.25f);
            Material matEnvFloor = CreateTexturedLitMaterial("Mat_EnvFloor", floorTex, 0.1f, 0.35f);
            Material matEnvProp = CreateTexturedLitMaterial("Mat_EnvProp", propTex, 0.08f, 0.3f);

            foreach (Renderer r in env.GetComponentsInChildren<Renderer>())
            {
                string n = r.gameObject.name.ToLowerInvariant();
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (n.Contains("floor") || n.Contains("ground") || n.Contains("plane"))
                        mats[i] = matEnvFloor;
                    else if (n.Contains("box") || n.Contains("crate") || n.Contains("barrel") || n.Contains("cube"))
                        mats[i] = matEnvProp;
                    else
                        mats[i] = (i % 2 == 0) ? matEnvWall : matEnvFloor;
                }
                r.sharedMaterials = mats;
            }
        }

        private static Texture2D CreateProceduralTexture(string texName, Color baseCol, Color accentCol, bool gridLines)
        {
            string dir = "Assets/GameAssets/Textures/Environment";
            Directory.CreateDirectory(dir);
            string path = $"{dir}/{texName}.png";

            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            System.Random rng = new System.Random(texName.GetHashCode());

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n = (float)rng.NextDouble() * 0.12f;
                    Color c = Color.Lerp(baseCol, accentCol, n * 4f);
                    if (gridLines && (x < 3 || y < 3 || x == size / 2 || y == size / 2))
                    {
                        c = Color.Lerp(c, accentCol, 0.65f);
                    }
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material CreateTexturedLitMaterial(string name, Texture2D tex, float metallic, float smoothness)
        {
            Material mat = CreateLitMaterial(name, Color.white, metallic, smoothness);
            if (tex != null && mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", tex);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
