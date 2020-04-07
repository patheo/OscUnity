using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

/* Ce module permet le controle d'un grand nombre de fonction de Unity directement en OSC
 *
 * Il nécéssite d'avoir un module OSC actif dans un objet.
 *
 * Voila les commandes reconnus :
 *
 * "/enable <nom du parent> <nom de la cible> <0/1>" 
 *
 * "/<nom de la cible>/transform/position" nécéssite 3 valeurs
 * "/<nom de la cible>/transform/rotate" nécéssite 3 valeurs
 * "/<nom de la cible>/transform/scale" nécéssite 3 valeurs
 * "/<nom de la sible>/material/<nom du parametre>" prend pour le moment uniquement 1 float
 *
 * "/<nom de la cible>/<nom du script>/<nom du parametre>" accepte les vector4,3,2, les float, int et bool
 *
 * Vous pouvez également activer les module de bloom et les param de camera en retirant les commentaire dans le script.
 *
 *
 * Script testé à plusieurs reprise avec l'excellent chef d'orchestre numérique Chataigne que je vous recommande chaudement.
 * 
 */

public class oscGetAdressUltim : MonoBehaviour
{

    // Necessite d'avoir quelque part dans son projet la librairie OSC
    public OSC osc;

    //Bloom bloomLayer = null; // Pour modifier les paramètres de post processing si besoin.

    void Start()
    {

        // Tout les messages partent dans la fonction allAdress. C'est elle qui fait le dispatch
        osc.SetAllMessageHandler(allAdress);

        // J'isole le message "/enable" qui est traité séparément dans une fonction enableObject.
        // Elle permet d'activer ou non des objets. Plus de détails directement dans la fonction...
        osc.SetAddressHandler("/enable", enableObject);

    }

    // FUNCTION POUR ACTIVER OU DESACTIVER DES OBJECTS

    void enableObject(OscMessage message) {
        // Pour activer ou deactivers des objets, j'ai prévu isi de toujours travailler des childrens.
        // Unity n'arrivant pas à trouver les objets désactivé dans le vide.
        // Il arrive par contre à trouver les objets enfant.
        // A en croire les forums, il existe une astuce pour éviter ce problème mais pour moi
        // ça me permet de trier mes objets donc je n'ai pas pousser dans ce sens pour le moment.

        // Dans ce cas là, la commande sera la suivante :
        // "/enable <nom du parent> <nom de l'objet cible> 1/0" 1 pour activer et 0 pour désactiver bien sûr.
        GameObject.Find(message.GetString(0)).transform.Find(message.GetString(1)).gameObject.SetActive(message.GetInt(2) != 0);
    }

    // FUNCITON PRINCIPALE
    void allAdress(OscMessage message)
    {
        // Transformation de l'adresse en liste de string pour la rendre lisible.
        // "/object/component/param" devient {" ", "object", "component", "param"}

        string[] add = message.address.Split('/');

        // Directement on récupère l'objet. C'est le plus simple.
        GameObject obj = GameObject.Find(add[1]);
        Component src;

        // Ici j'ai séparé des éléments qiu ne sont pas des component à proprement parlé pour les avoir accessible aussi.
        // J'ai isolé : material, transform, camera, et bloom. C'est se dont j'avais besoin et il est possible que j'en rajoute avec le temps.

        if (obj)
        {
          switch(add[2]) {

            // Pour material, rien de compliqué, attention de bien nommé la variable de destination avec son nom de shader (souvent commençant par _).
            case "material": obj.GetComponent<Renderer>().material.SetFloat(add[3], message.GetFloat(0)); break;

            // Pour le moment pour transform, j'ai isolé les principales fonctions : position, rotation en degrés et scale.
            case "transform":
                switch(add[3]) {
                  case "position" : obj.transform.localPosition = new Vector3(message.GetFloat(0), message.GetFloat(1), message.GetFloat(2)); break;
                  case "rotate" : obj.transform.eulerAngles = new Vector3(message.GetFloat(0), message.GetFloat(1), message.GetFloat(2)); break;
                  case "scale" : obj.transform.localScale = new Vector3(message.GetFloat(0), message.GetFloat(1), message.GetFloat(2)); break;
                }; break;

            /*
            // Quelques éléments de la camera
            case "camera":
                switch(add[3]) {
                  case "far" : obj.GetComponent<Camera>().farClipPlane = message.GetFloat(0); break;
                  case "near" : obj.GetComponent<Camera>().nearClipPlane = message.GetFloat(0); break;
                  case "postProcess" : obj.GetComponent<Camera>().nearClipPlane = message.GetFloat(0); break;

                }; break;

            // Quelques param du bloom pour les besoins de la création sur laquelle je travaille.
            case "bloom":
                obj.GetComponent<PostProcessVolume>().profile.TryGetSettings(out bloomLayer);
                switch(add[3]){
                  case "intensity" : bloomLayer.intensity.value = message.GetFloat(0); break;
                  case "threshold" : bloomLayer.threshold.value = message.GetFloat(0); break;
                  case "diffusion" : bloomLayer.diffusion.value = message.GetFloat(0); break;
                  //case "color" : bloomLayer.color.value = message.GetFloat(0); break;
                }; break;
            */

            default: src = obj.GetComponent(add[2]);

                    if(src)
                    {
                      //Debug.Log(src.GetType().GetField(add[3]).GetValue(src));

                        object param = src.GetType().GetField(add[3]).GetValue(src);

                        if(param is Vector4)
                        {
                            src.GetType().GetField(add[3]).SetValue(src, new Vector4(message.GetFloat(0), message.GetFloat(1), message.GetFloat(2), message.GetFloat(3)));
                        }
                        else if(param is Vector3)
                        {
                            src.GetType().GetField(add[3]).SetValue(src, new Vector3(message.GetFloat(0), message.GetFloat(1), message.GetFloat(2)));
                        }
                        else if(param is Vector2)
                        {
                            src.GetType().GetField(add[3]).SetValue(src, new Vector2(message.GetFloat(0), message.GetFloat(1)));
                        }
                        else if(param is float)
                        {
                            src.GetType().GetField(add[3]).SetValue(src, message.GetFloat(0));
                        }
                        else if(param is int)
                        {
                            src.GetType().GetField(add[3]).SetValue(src, (int)message.GetFloat(0));
                        }
                        else if(param is bool)
                        {
                            src.GetType().GetField(add[3]).SetValue(src, ((int)message.GetFloat(0) == 1));
                        }
                        else
                        {
                            Debug.Log("Je ne connais pas ce type de data...");
                        }

                    }
                    break;

          }
        }
    }
}
