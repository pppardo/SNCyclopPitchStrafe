using HarmonyLib;
using mset;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RootMotion.FinalIK.RagdollUtility;
using UnityEngine;
using System.Reflection;


namespace pppardo.CyclopPitchStrafe {

    [HarmonyPatch(typeof(SubControl))]
    public class SubControlPatch {
        
        private static readonly FieldInfo _throttleInfo = GetPrivateSubControlMemberInfo("throttle");
        private static readonly FieldInfo _canAccelInfo = GetPrivateSubControlMemberInfo("canAccel");
        private static readonly FieldInfo _subInfo = GetPrivateSubControlMemberInfo("sub");


        private static FieldInfo GetPrivateSubControlMemberInfo(string _memberName) {
            //CyclopPitchStrafePlugin.logger.LogDebug("[Cyclop Strafe plugin] Obteniendo info del metodo " + _memberName);
            return typeof(SubControl).GetField(_memberName, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private static bool strafing = false;
        private static bool pitching = false;
        private static float pitch = 0f;
        private static float strafe = 0f;

        /* Correspondencia con objetos auxiliares */
        private static Dictionary<SubControl, SubControlPatch> auxiliarSubControls = new Dictionary<SubControl, SubControlPatch>();
        
        private static SubControlPatch getAuxControl(SubControl instance) {
            if(!auxiliarSubControls.TryGetValue(instance, out var patch)){
                patch=new SubControlPatch();
                auxiliarSubControls.Add(instance, patch);
                CyclopPitchStrafePlugin.logger.LogInfo(string.Format("Auxiliar control added. Total:{0}",auxiliarSubControls.Count));
            }
            return patch;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPatch(typeof(SubControl), "FixedUpdate")]
        [HarmonyPostfix]
        public static void FixedUpdatePost(SubControl __instance) { // Se ejecuta después de cada ejecución de FixedUpdate ( Ajuste de la actualización ) 
            try {

                if (__instance == null) return;
                //CyclopPitchStrafePlugin.logger.LogInfo(string.Format("__instance No nula."));

                if (!__instance.LOD.IsFull()) return;
                //CyclopPitchStrafePlugin.logger.LogInfo(string.Format("LOD full."));

                if (__instance.powerRelay.GetPowerStatus() == PowerSystem.Status.Offline) return;
                //CyclopPitchStrafePlugin.logger.LogInfo(string.Format("Power online."));

                if (Ocean.GetDepthOf(__instance.gameObject) <= 0f) return;
                //CyclopPitchStrafePlugin.logger.LogInfo(string.Format("Submerged."));

                //Vector3 throttle = (Vector3)_throttleInfo.GetValue(__instance);

                Rigidbody rb = __instance.GetComponent<Rigidbody>();
               
                // Control is moving
                //if ((double)Mathf.Abs(throttle.inclinacion) <= 0.0001)
                //    return;
                
                SubControlPatch aux = getAuxControl(__instance);
                
                bool canAccel = (bool)_canAccelInfo.GetValue(__instance);
                if (strafing) {
                    //CyclopPitchStrafePlugin.logger.LogInfo(string.Format("Strafing."));

                    float num = __instance.BaseVerticalAccel;
                    num += (float)__instance.gameObject.GetComponentsInChildren<BallastWeight>().Length * __instance.AccelPerBallast;
                    if (canAccel) {
                        rb.AddForce(-__instance.transform.right * num * __instance.accelScale * strafe * CyclopPitchStrafePlugin.MyConfig.StrafeSpeed, ForceMode.Acceleration);
                    }
                }
                if (pitching) {
                    //CyclopPitchStrafePlugin.logger.LogInfo(string.Format("Pitching."));
                    Stabilizer stabilizer = __instance.GetComponent<Stabilizer>();
                    if (stabilizer != null) {
                        stabilizer.stabilizerEnabled = false;
                        //CyclopPitchStrafePlugin.logger.LogInfo(string.Format("DesActivando estabilizador"));
                    } else {
                        CyclopPitchStrafePlugin.logger.LogInfo(string.Format("Stabilizer to disable not found."));
                    }
                    float baseTurningTorque = __instance.BaseTurningTorque;
                    SubRoot _sub = (SubRoot)_subInfo.GetValue(__instance);
                    if (canAccel) {
                        rb.AddTorque(_sub.subAxis.right * baseTurningTorque * __instance.turnScale * pitch, ForceMode.Acceleration);
                    }
                }

            } catch (System.Exception _e) {
                CyclopPitchStrafePlugin.logger.LogError("Error in mod: " + _e.Message);
                CyclopPitchStrafePlugin.logger.LogError("\n" + _e.StackTrace);
                CyclopPitchStrafePlugin.IsError = true;
            }

            // Fin 
        }

        [HarmonyPatch(typeof(SubControl), "Update")]
        [HarmonyPrefix]
        public static bool UpdatePre(SubControl __instance) {
            if (KeyHandler.CancelMovement) {
                GameInput.ClearInput();
            }

            return true;
        }

            /// <summary>
            /// Parchea el método Update que se llama en cada Frame
            /// </summary>
            /// <param name="__instance"></param>    Objeto original
            [HarmonyPatch(typeof(SubControl), "Update")]
        [HarmonyPostfix]
        public static void UpdatePost(SubControl __instance) {

            if (__instance == null) return;

            if (!__instance.LOD.IsFull()) return;

            if (__instance.controlMode != SubControl.Mode.DirectInput) return;

            SubControlPatch aux = getAuxControl(__instance);
            strafing = false;
            pitching = false;

            var canAccel = (bool)_canAccelInfo.GetValue(__instance);
            //var throttle = (Vector3)_throttleInfo.GetValue(__instance);

            if (KeyHandler.StrafeLeft) {
                strafe = -1f;
                strafing = true;
            } else if (KeyHandler.StrafeRight) {
                strafe = 1f;
                strafing = true;
            }
            if (KeyHandler.PitchUp) {
                pitch = -1f;
                pitching = true;
            } else if (KeyHandler.PitchDown) {
                pitch = 1f;
                pitching = true;
            }
            //_throttleInfo.SetValue(__instance, throttle);
            if (KeyHandler.Stabilizer) {
                Stabilizer stabilizer = __instance.GetComponent<Stabilizer>();
                if (stabilizer != null) {
                    StabilizerPatch.EmergencyStablilicerEnable(stabilizer);
                } else {
                    CyclopPitchStrafePlugin.logger.LogInfo(string.Format("Stabilizer not found."));

                }
            }
        }
    } // Fin de la clase
    /// <summary>
    /// Parcheo de la clase Player para el boton de estabilización de emergencia
    /// </summary>
    [HarmonyPatch(typeof(Player))]
    public static class PlayerPatch {
        [HarmonyPatch(typeof(Player), "Update")]
        [HarmonyPostfix]
        public static void UpdatePlayerPostfix(Player __instance) {
            if (__instance.isPiloting)
                return;
            if (__instance.IsInSubmarine()) {
                Stabilizer stabilizer = __instance.currentSub.GetComponent<Stabilizer>();
                if (!stabilizer.stabilizerEnabled && KeyHandler.Stabilizer) {
                    StabilizerPatch.EmergencyStablilicerEnable(stabilizer);
                }
            }
        }
    }

    /// <summary>
    /// Parcheo de la clase Stabilizer para restablecer la aceleración
    /// </summary>
    [HarmonyPatch(typeof(Stabilizer))]
    public static class StabilizerPatch {
        [HarmonyPatch(typeof(Stabilizer), "FixedUpdate")]
        [HarmonyPostfix]
        public static void UpdatePlayerPostfix(Stabilizer __instance) {
            if (!__instance.stabilizerEnabled || __instance.body == null || __instance.body.isKinematic) 
                return;
            if (__instance.uprightAccelerationStiffness != 10f && Mathf.Abs(Mathf.DeltaAngle(__instance.body.transform.eulerAngles.x,0f))<0.001f) {
                __instance.uprightAccelerationStiffness = 10f;
                CyclopPitchStrafePlugin.logger.LogInfo(string.Format("Reset stabilizer.({0})", __instance.uprightAccelerationStiffness));
            }
        }
        public static void EmergencyStablilicerEnable(Stabilizer stabilizer) {
            stabilizer.stabilizerEnabled = true;
            stabilizer.uprightAccelerationStiffness = CyclopPitchStrafePlugin.MyConfig.StabilizerSpeed;
            CyclopPitchStrafePlugin.logger.LogInfo(string.Format("Activating emergency stabilizer.({0})", CyclopPitchStrafePlugin.MyConfig.StabilizerSpeed));
        }
    }


    /// <summary>
    /// Parcheo para indicar la inclinación
    /// </summary>
    [HarmonyPatch(typeof(CyclopsHelmHUDManager))]
    public static class CyclopsHelmHUDManagerPatch {
        private static readonly FieldInfo _gyroSpinSpeed = typeof(Stabilizer).GetField("gyroSpinSpeed", BindingFlags.Instance | BindingFlags.NonPublic);
        private static int i = 0;
        [HarmonyPatch(typeof(CyclopsHelmHUDManager), "Update")]
        [HarmonyPostfix]
        public static void UpdatePostfix(CyclopsHelmHUDManager __instance) {
            if (!__instance.LOD.IsFull())
                    return;
            if (__instance.subLiveMixin.IsAlive()) {
                /*if (i++ > 60) {
                    i = 0;
                    CyclopPitchStrafePlugin.logger.LogInfo(string.Format("1 de 60-Actualizando Icon"));
                }*/
                float z = __instance.subRoot.rb.transform.eulerAngles.z;
                float inclinacion = __instance.subRoot.rb.transform.eulerAngles.x;
                if (Mathf.Abs(180-z) < 90) { // Inverted
                    inclinacion = z - inclinacion;
                }
                
                //if (i == 60) CyclopPitchStrafePlugin.logger.LogInfo(string.Format("1 de 60-Actualizando Icon Inclinacion({0})",inclinacion));
                //GameObject.Find("Landscape/Global Root/Cyclops-MainPrefab(Clone)/HelmHUD/HelmHUDVisuals/Canvas_LeftStatusHUD/SubIconWarnings/");
                if (i == 60) {
                    var t = __instance.gameObject.transform;
                    for (int i = 0; i < t.childCount; i++) {
                        CyclopPitchStrafePlugin.logger.LogInfo(string.Format("1 de 60-Actualizando Icon Child({0})= {1}", i, t.GetChild(i).name));
                    }
                    GameObject gameObject = t.Find("HelmHUDVisuals").gameObject;
                    CyclopPitchStrafePlugin.logger.LogInfo(string.Format("1 de 60-Actualizando Icon HelmHUDVisuals nulo {0}", gameObject==null));
                    gameObject = t.Find("HelmHUDVisuals/Canvas_LeftStatusHUD/SubIconWarnings/SubIcon").gameObject;
                    CyclopPitchStrafePlugin.logger.LogInfo(string.Format("1 de 60-Actualizando Icon SubIcon nulo {0}", gameObject==null));
                }
                GameObject subIcon = __instance.gameObject.transform.Find("HelmHUDVisuals/Canvas_LeftStatusHUD/SubIconWarnings/SubIcon").gameObject;
                if (i == 60) CyclopPitchStrafePlugin.logger.LogInfo(string.Format("1 de 60-Actualizando Icon subIcon encontrado {0}",subIcon!=null));
                RectTransform rectTransform = subIcon.GetComponent<RectTransform>();
                if (i == 60) CyclopPitchStrafePlugin.logger.LogInfo(string.Format("1 de 60-Actualizando Icon RectTransform encontrado ({0})", rectTransform!=null));
                rectTransform.localEulerAngles = new Vector3(0,0, 360-inclinacion);

            }
        }
    }

}
