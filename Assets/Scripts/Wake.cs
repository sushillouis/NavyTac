using UnityEngine;
using System.Collections;

public class Wake : MonoBehaviour {

    public ParticleSystem wakeParticleSystem;
    float wakeThresholdSpeed = 0.77f;
    public float wakeLengthFactor = 0.13f;

    public float wakeSpeed;

    public Gradient dayGradient;
    public Gradient nightGradient;
    public Gradient duskGradient;

    public ParticleSystem.MinMaxGradient dayMMGradient;
    public ParticleSystem.MinMaxGradient nightMMGradient;
    public ParticleSystem.MinMaxGradient duskMMGradient;

    public Entity entity;
    // Use this for initialization
    void Start () {
        wakeParticleSystem.Stop();
        entity = GetComponentInParent<Entity>();

        dayMMGradient = new ParticleSystem.MinMaxGradient(dayGradient);
        nightMMGradient = new ParticleSystem.MinMaxGradient(nightGradient);
        duskMMGradient = new ParticleSystem.MinMaxGradient(duskGradient);

    }

    bool isNight()
    {
        return false;
    }

    bool isDusk()
    {
        return false;
    }

    public bool shouldDisappear = false;
    
    public void Disappear()
    {
        var cofm = wakeParticleSystem.colorOverLifetime;
        cofm.color = new ParticleSystem.MinMaxGradient(Color.clear);
    }



	// Update is called once per frame
	void Update () {
        if(!entity.isVisible){
            wakeParticleSystem.Stop();
            return;
        }
        if (entity.speed <= wakeThresholdSpeed) {
            wakeParticleSystem.Stop();
        } else if (!wakeParticleSystem.isPlaying) {
            wakeParticleSystem.Play();
        }
        wakeSpeed = entity.speed * wakeLengthFactor;

        var cofm = wakeParticleSystem.colorOverLifetime;
        
        // Missile wake color handling
        if (entity.entityClass == EntityClass.Missile)
        {
            if (entity.owner != null)
            {
                // Create solid color gradient from owner's color
                cofm.color = new ParticleSystem.MinMaxGradient(entity.owner.playerColor);
            }
        }
        else if (shouldDisappear)
        {
            Disappear();
        }
        else 
        {
            // Existing time-of-day handling
            if (isNight()) {
                cofm.color = nightMMGradient;
            } 
            else if (isDusk()) {
                cofm.color = duskMMGradient;
            } 
            else {
                cofm.color = dayMMGradient;
            }
        }
    }



}
