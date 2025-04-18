using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class CollisionManager : MonoBehaviourPun
{
    public static CollisionManager Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this.gameObject);
    }

    /// <summary>
    /// Handles logic when two attacks collide. Only executed by owners of the attack GameObjects.
    /// </summary>
    public void ResolveAttackCollision(AttackScript attackA, AttackScript attackB)
    {
        // Only allow the owner of one of the attacks to resolve (e.g., lowest viewID owner)
        if (!attackA.photonView.IsMine && !attackB.photonView.IsMine) return;

        // Consistently resolve only from one side
        if (attackA.photonView.ViewID > attackB.photonView.ViewID) return;

        float aPower = attackA.currPower;
        float bPower = attackB.currPower;

        float aResult = aPower - bPower;
        float bResult = bPower - aPower;

        Debug.Log($"[CollisionManager] Attack {attackA.photonView.ViewID} vs Attack {attackB.photonView.ViewID}");
        Debug.Log($"  A Power: {aPower}, B Power: {bPower}");

        // Damage & destroy via RPC
        if (aResult <= 0)
        {
            attackA.photonView.RPC("RPC_DestroyAttack", RpcTarget.All);
        }
        else
        {
            attackA.photonView.RPC("RPC_UpdatePower", RpcTarget.All, aResult);
        }

        if (bResult <= 0)
        {
            attackB.photonView.RPC("RPC_DestroyAttack", RpcTarget.All);
        }
        else
        {
            attackB.photonView.RPC("RPC_UpdatePower", RpcTarget.All, bResult);
        }
    }
    public void ResolvePlayerHit(PlayerScript player, AttackScript attack)
    {
        if (!attack.photonView.IsMine) return; // Only the attack owner should resolve

        float damage = attack.currPower;

        Debug.Log($"[CollisionManager] Attack {attack.photonView.ViewID} hit Player {player.view.ViewID} for {damage} damage");

        // Damage the player
        player.view.RPC("RPC_TakeDamage", RpcTarget.All, damage);

        // Destroy the attack for everyone
        attack.photonView.RPC("RPC_DestroyAttack", RpcTarget.All);
    }
}
