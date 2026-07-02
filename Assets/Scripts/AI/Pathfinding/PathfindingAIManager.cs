using RTS.AI.Pathfinding.UnitPathfindingController;
using RTS.Core;
using RTS.SystemManagement.GridSystem;
using RTS.Units.Common;
using RTS.Units.Common.States;
using RTS.World.WorldManagement;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.AI.Pathfinding
{
    public class PathfindingAIManager
    {
        public PlayerInfo playerInfo;

        private TileDatabase tileDatabase;

        private List<SingleUnitMovement> singleUnitMovementList = new List<SingleUnitMovement>();
        private List<GroupUnitMovement> groupUnitMovementList = new List<GroupUnitMovement>();

        private int minimumGroupUnitMovement = 5;
        private int singleUnitMovementCount;
        private int groupUnitMovementCount;

        private float separationDistance = 0.6f;

        #region Initialization

        public PathfindingAIManager(PlayerInfo owner)
        {
            playerInfo = owner;
            tileDatabase = playerInfo.GameManager.WorldManager.tileDatabase;
        }

        #endregion

        #region Registration

        private void RegisterSingleUnitMovement(List<BaseUnitController> units, Vector3 destination)
        {
            foreach (BaseUnitController unit in units)
            {
                singleUnitMovementCount++;

                SingleUnitMovement singleUnitMovement = new SingleUnitMovement();

                singleUnitMovement.id = singleUnitMovementCount;
                singleUnitMovement.unit = unit;
                singleUnitMovement.destination = destination;
                singleUnitMovement.vectorPath = GenerateAStarPathfinding(unit, destination);
                singleUnitMovement.pathProgress = 0;
                singleUnitMovement.isDoneMoving = false;

                singleUnitMovementList.Add(singleUnitMovement);
                unit.singleUnitMovement = singleUnitMovement;

                unit.OnUnitMoveReached += Unit_OnUnitMoveReached;
            }
        }

        private void RegisterGroupUnitMovement(List<BaseUnitController> units, Vector3 destination)
        {
            groupUnitMovementCount++;

            GroupUnitMovement groupUnitMovement = new GroupUnitMovement();

            groupUnitMovement.id = groupUnitMovementCount;
            groupUnitMovement.units = units;
            groupUnitMovement.destination = destination;
            groupUnitMovement.flowfieldGrid = GenerateFlowfieldPathfinding(destination);
            groupUnitMovement.isDoneMoving = false;

            groupUnitMovementList.Add(groupUnitMovement);

            foreach (var unit in units)
            {
                unit.groupUnitMovement = groupUnitMovement;
                unit.OnUnitMoveReached += Unit_OnUnitMoveReached;
            }
        }

        private void UnregisterUnitMovement(BaseUnitController unit)
        {
            unit.singleUnitMovement.id = 0;
            unit.singleUnitMovement.isDoneMoving = true;

            unit.groupUnitMovement.id = 0;
            unit.groupUnitMovement.isDoneMoving = true;

            unit.OnUnitMoveReached -= Unit_OnUnitMoveReached;
        }

        private void Unit_OnUnitMoveReached(BaseUnitController unit)
        {
            UnregisterUnitMovement(unit);
        }

        #endregion

        #region Pathfinding System Determination

        public void SetMoveTo(List<BaseUnitController> units, Vector3 destination, Action doAfterReached = null)
        {
            if (units.Count >= minimumGroupUnitMovement)
            {
                SetToFlowfieldPathfinding(units, destination);
            } else
            {
                SetToAStarPathfinding(units, destination);
            }

            foreach (var unit in units)
            {
                unit.ChangeState(new MoveState(destination, doAfterReached));
            }
        }

        private void SetToAStarPathfinding(List<BaseUnitController> units, Vector3 destination)
        {
            RegisterSingleUnitMovement(units, destination);
        }

        private void SetToFlowfieldPathfinding(List<BaseUnitController> units, Vector3 destination)
        {
            RegisterGroupUnitMovement(units, destination);
        }

        private List<Vector3> GenerateAStarPathfinding(BaseUnitController unit, Vector3 destination)
        {
            return AStarGenerator.Instance.FindPath(unit.transform.position, destination);
        }

        private Grid<PathNode> GenerateFlowfieldPathfinding(Vector3 destination)
        {
            return FlowfieldGenerator.Instance.GenerateFlowField(destination);
        }

        #endregion

        #region Handle Movement

        public void HandleMovement(BaseUnitController unit)
        {
            if (unit.singleUnitMovement.id != 0 && unit.singleUnitMovement.isDoneMoving != true)
            {
                AStarMove(unit);
            }
            else if (unit.groupUnitMovement.id != 0 && unit.groupUnitMovement.isDoneMoving != true)
            {
                FlowfieldMove(unit);
            }
            else { return; }
        }

        private void AStarMove(BaseUnitController unit)
        {
            List<Vector3> vectorPath;
            if (unit.singleUnitMovement.vectorPath == null)
            {
                Debug.LogWarning("This unit doesnt have astar vectorpath");
                return;
            }
            else { vectorPath = unit.singleUnitMovement.vectorPath; }

            if (unit.singleUnitMovement.pathProgress >= unit.singleUnitMovement.vectorPath.Count
                && !unit.IsDestinationInRange(unit.singleUnitMovement.destination, 0.05f))
            {
                unit.transform.position = Vector3.MoveTowards(unit.transform.position, unit.singleUnitMovement.destination, unit.GetUnitInfo().moveSpeed * Time.deltaTime);

                if (tileDatabase.GetImpassibleTilemapInRadius(unit.transform.position, 1f).Count > 0)
                {
                    unit.StopMovement();
                }

                return;
            } else if (unit.IsDestinationInRange(unit.singleUnitMovement.destination, 0.05f))
            {
                return;
            }

            Vector3 unitPos = unit.transform.position;
            unitPos.z = 0;

            Vector3 path = vectorPath[unit.singleUnitMovement.pathProgress];
            path.z = 0;

            float moveSpeed = unit.GetUnitInfo().moveSpeed;

            unit.transform.position = Vector3.MoveTowards(unitPos, path, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(unit.transform.position, path) < 0.05f)
                unit.singleUnitMovement.pathProgress++;
        }

        private void FlowfieldMove(BaseUnitController unit)
        {
            Grid<PathNode> flowfieldGrid;
            if (unit.groupUnitMovement.flowfieldGrid == null)
            {
                Debug.LogWarning("This unit doesnt have flowfield grid");
                return;
            }

            // Get unit position
            flowfieldGrid = unit.groupUnitMovement.flowfieldGrid;
            Vector2 unitPos = unit.transform.position;
            PathNode nodeBelow = flowfieldGrid.GetGridObject(unitPos);

            // Get move position
            float moveSpeed = unit.GetUnitInfo().moveSpeed;
            Vector2 moveDirection = new Vector2(nodeBelow.bestDirection.Vector.x, nodeBelow.bestDirection.Vector.y);
            Vector2 moveToPos = new Vector2(unitPos.x, unitPos.y) + moveDirection * moveSpeed * Time.fixedDeltaTime;
            unit.transform.position = new Vector3(moveToPos.x, moveToPos.y, 0);
        }

        #endregion

        #region Unit Collision Avoidance

        public void HandleUnitCollisionAvoidance(BaseUnitController unit)
        {
            var neighbours = unit.GetNeighbourUnitNearby(5f);

            if (neighbours == null || neighbours.Count <= 1)
                return;

            Vector2 steer = CalculateSeparation(unit, neighbours);

            /*List<Vector2> impassibleTilemaps = tileDatabase.GetImpassibleTilemapInRadius(unit.transform.position, 5f);

            if (impassibleTilemaps != null && impassibleTilemaps.Count > 0)
            {
                steer += CalculateSeparationFromImpassibleTilemaps(unit, impassibleTilemaps);
            }*/

            if (steer.sqrMagnitude > 0)
            {
                steer = steer.normalized * unit.GetUnitInfo().moveSpeed;
                Vector2 steerForce = steer - unit.velocity;

                steerForce = Vector2.ClampMagnitude(steerForce, unit.maxForce);
                unit.ApplyForce(steerForce);
            }
        }

        private Vector2 CalculateSeparation(BaseUnitController unit, List<BaseUnitController> neighbours)
        {
            Vector2 totalSteer = Vector2.zero;
            int count = 0;
            float sqrSeparation = separationDistance * separationDistance;

            foreach (BaseUnitController other in neighbours)
            {
                if (other == unit) continue;

                Vector2 diff = (Vector2)(unit.transform.position - other.transform.position);
                float sqrDist = diff.sqrMagnitude;

                if (sqrDist < sqrSeparation)
                {
                    if (sqrDist <= 0.0001f)
                    {
                        float randomAngle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                        diff = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
                    }
                    else
                    {
                        diff.Normalize();
                        diff /= Mathf.Sqrt(sqrDist);
                    }

                    totalSteer += diff;
                    count++;
                }
            }

            if (count > 0)
            {
                totalSteer /= count;
            }

            return totalSteer;
        }

        private Vector2 CalculateSeparationFromImpassibleTilemaps(BaseUnitController unit, List<Vector2> impassibleTilemaps)
        {
            Vector2 totalSteer = Vector2.zero;
            int count = 0;
            float sqrSeparation = (separationDistance * separationDistance) * 8;

            foreach (Vector2 tilePos in impassibleTilemaps)
            {
                Vector2 diff = (Vector2)(unit.transform.position - new Vector3(tilePos.x, tilePos.y, 0));
                float sqrDist = diff.sqrMagnitude;

                if (sqrDist < sqrSeparation)
                {
                    if (sqrDist <= 0.0001f)
                    {
                        float randomAngle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                        diff = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
                    }
                    else
                    {
                        diff.Normalize();
                        diff /= Mathf.Sqrt(sqrDist);
                    }

                    totalSteer += diff;
                    count++;
                }
            }

            if (count > 0)
            {
                totalSteer /= count;
            }

            return totalSteer;
        }

        #endregion
    }

    #region Struct

    public struct SingleUnitMovement
    {
        public int id;
        public BaseUnitController unit;
        public Vector3 destination;
        public List<Vector3> vectorPath;
        public int pathProgress;
        public bool isDoneMoving;
    }

    public struct GroupUnitMovement
    {
        public int id;
        public List<BaseUnitController> units;
        public Vector3 destination;
        public Grid<PathNode> flowfieldGrid;
        public bool isDoneMoving;
    }

    #endregion
}