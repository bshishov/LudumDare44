using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class InputSystem : JobComponentSystem
{
    [BurstCompile]
    struct PlayerInputJob : IJobForEach<InputComponent, Translation>
    {
        public float DeltaTime;

        public void Execute(ref InputComponent data, ref Translation pos)
        {
            float3 moveVector = new Vector3(data.Horisontal, 0, data.Vertical).normalized * 3 * DeltaTime;
            pos.Value = pos.Value + moveVector;
        }
    }
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new PlayerInputJob()
        {
            DeltaTime = Time.deltaTime
        };

        return job.Schedule(this, inputDependencies);
    }
}