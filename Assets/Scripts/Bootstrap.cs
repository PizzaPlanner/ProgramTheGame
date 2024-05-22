// using Unity.Entities;
// using UnityEngine.SceneManagement;
//
// namespace DefaultNamespace
// {
//     public class Bootstrap : ICustomBootstrap
//     {
//         public bool Initialize(string defaultWorldName)
//         {
//             DefaultWorldInitialization.Initialize(defaultWorldName);
//             if(!SceneManager.GetSceneByName("Entry").isLoaded)
//                 SceneManager.LoadScene("Entry", LoadSceneMode.Additive);
//             return true;
//         }
//     }
// }