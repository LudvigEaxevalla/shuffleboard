using Godot;
using System;

public interface IProjectile
{
    RigidBody2D PhysicsBody { get; }
    int MinProjectileForce { get; }
    int MaxProjectileForce { get; }
    bool CanBeLaunched { get; }
    bool HasProjectileStopped { get; }
    void OnProjectileReleased();
    void OnProjectileStopped();
    int[] identifier { get; set; }
}

public partial class HandleProjectiles : Node2D
{
    private bool stopHandled;
    private StateGame stateGame;
    private enum State
    {
        Idle,
        BuildingForce,
        Released,
        Stopped
    }

    private IProjectile projectile;
    private State currentState = State.Idle;
    private bool forceHitMax;
    private float timeSinceRelease;
    public int ForceBuildUp { get; private set; }

    public override void _Ready()
    {
        stateGame = GetTree().CurrentScene.GetNode<StateGame>("STATE_GAME");
        projectile = GetParent() as IProjectile;
        if (projectile == null)
        {
            SetProcess(false);
            return;
        }

        ForceBuildUp = projectile.MinProjectileForce;
    }

    public override void _Process(double delta)
    {
        switch (currentState)
        {
            case State.Idle:
                if (projectile.CanBeLaunched && Input.IsActionJustPressed("build_up"))
                {
                    ForceBuildUp = projectile.MinProjectileForce;
                    forceHitMax = false;
                    currentState = State.BuildingForce;
                }
                break;
            case State.BuildingForce:
                if (Input.IsActionPressed("build_up"))
                {
                    BuildForce();
                }
                else if (Input.IsActionJustReleased("build_up"))
                {
                    Release();
                    currentState = State.Released;
                }
                break;
            case State.Released:
                timeSinceRelease += (float)delta;
                var body = projectile.PhysicsBody;
                if (timeSinceRelease > 0.1f && (body.Sleeping || body.LinearVelocity.Length() <= 0.1f))
                    currentState = State.Stopped;
                break;
            case State.Stopped:
                if (!stopHandled)
                {
                    if (!projectile.HasProjectileStopped)
                        projectile.OnProjectileStopped();

                    stateGame.OnProjectileStopped();
                    stopHandled = true;
                }
                break;
        }
    }

    private void BuildForce()
    {
        if (forceHitMax)
        {
            ForceBuildUp -= 5;
            if (ForceBuildUp <= projectile.MinProjectileForce)
            {
                ForceBuildUp = projectile.MinProjectileForce;
                forceHitMax = false;
            }
        }
        else
        {
            ForceBuildUp = Math.Max(ForceBuildUp, projectile.MinProjectileForce) + 5;
            if (ForceBuildUp >= projectile.MaxProjectileForce)
            {
                ForceBuildUp = projectile.MaxProjectileForce;
                forceHitMax = true;
            }
        }
    }

    private void Release()
    {
        var body = projectile.PhysicsBody;
        var arrow = body.GetNode<Arrow>("../Arrow");
        var direction = Vector2.FromAngle(arrow.GlobalRotation);
        if (projectile.identifier == null)
        {
            projectile.identifier = new[] { stateGame.GetNextProjectileIdentifier() };
            GD.Print("Projectile identifier set to: " + projectile.identifier[0]);
        }
        projectile.OnProjectileReleased();
        timeSinceRelease = 0;
        body.ApplyCentralImpulse(new Vector2(
            ForceBuildUp * direction.X,
            ForceBuildUp * direction.Y));
    }
}
