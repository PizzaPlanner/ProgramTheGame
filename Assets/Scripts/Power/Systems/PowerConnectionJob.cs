using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Power
{
    [WithAll(typeof(PowerCable))]
    [WithNone(typeof(Ghost))]
    [WithAll(typeof(PowerCable.NeedsRecalculation))]
    public partial struct PowerConnectionJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<PowerNetwork> networkLookup;
        public ComponentLookup<PowerCable> cableLookup;
        public EntityCommandBuffer ecb;
        void Execute(Entity e, in DynamicBuffer<Neighbours> neighbours)
        {
            var changed = false;
            if(!cableLookup.TryGetComponent(e, out var cable))
                return;
            foreach (var nb in neighbours)
            {
                if(!cableLookup.TryGetComponent(nb.Other, out var otherCable))
                    continue;
                if(otherCable.Network == cable.Network)
                    continue;
                // Something deferred, need to resolve later
                if(otherCable.Network.Index < 0 || cable.Network.Index < 0)
                    continue;
                var isSelfOk = networkLookup.TryGetComponent(cable.Network, out var selfNw);
                var isOtherOk = networkLookup.TryGetComponent(otherCable.Network, out var otherNw);
                if (!isSelfOk && isOtherOk)
                {
                    cable.Network = otherCable.Network;
                    cableLookup[e] = cable;
                    ecb.SetComponentEnabled<PowerCable.NeedsRecalculation>(e, true);
                    changed = true;
                }
                else if (isSelfOk && !isOtherOk)
                {
                    otherCable.Network = cable.Network;
                    cableLookup[nb.Other] = otherCable;
                    ecb.SetComponentEnabled<PowerCable.NeedsRecalculation>(nb.Other, true);
                }
                else if (isSelfOk && isOtherOk)
                {
                    if (selfNw.Id > otherNw.Id)
                    {
                        otherCable.Network = cable.Network;
                        cableLookup[nb.Other] = otherCable;
                        ecb.SetComponentEnabled<PowerCable.NeedsRecalculation>(nb.Other, true);
                    }
                    else
                    {
                        cable.Network = otherCable.Network;
                        cableLookup[e] = cable;
                        ecb.SetComponentEnabled<PowerCable.NeedsRecalculation>(e, true);
                        changed = true;
                    }
                }
                else
                {
                    var newNw = ecb.CreateEntity();
                    ecb.AddComponent(newNw, PowerNetwork.Next);
                    var newRef = new PowerCable { Network = newNw };
                    // Set lookup for following checks in same job,
                    // set ecb to enforce deferred entity resolution 
                    cableLookup[e] = newRef;
                    cableLookup[nb.Other] = newRef;
                    ecb.SetComponent(e, newRef);
                    ecb.SetComponent(nb.Other, newRef);
                    ecb.SetComponentEnabled<PowerCable.NeedsRecalculation>(nb.Other, true);
                    ecb.SetComponentEnabled<PowerCable.NeedsRecalculation>(e, true);
                    changed = true;
                }
                
            }

            if (cable.Network == default)
            {
                var newNw = ecb.CreateEntity();
                ecb.AddComponent(newNw, PowerNetwork.Next);
                var newRef = new PowerCable { Network = newNw };
                cableLookup[e] = newRef;
                ecb.SetComponent(e, newRef);
                ecb.SetComponentEnabled<PowerCable.NeedsRecalculation>(e, true);
                changed = true;
            }
            
            if(!changed)
                ecb.SetComponentEnabled<PowerCable.NeedsRecalculation>(e, false);
        }
    }
}