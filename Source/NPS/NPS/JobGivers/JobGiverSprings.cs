﻿using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

// NOTE: The job that puts pawns in springs when they're too hot is in harmonypatches/jobgiverspringspatch.cs



namespace TKKN_NPS
{
	public class JobGiver_GoSwimming : JobGiver_Wander
	{

		protected float wanderRadius;

		private static List<IntVec3> swimmingSpots = new List<IntVec3>();

		protected Func<Pawn, IntVec3, bool> wanderDestValidator;

		protected IntRange ticksBetweenWandersRange = new IntRange(20, 100);

		protected LocomotionUrgency locomotionUrgency = LocomotionUrgency.Walk;

		protected Danger maxDanger = Danger.None;

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!JoyUtility.EnjoyableOutsideNow(pawn, null))
			{
				return null;
			}
			bool nextMoveOrderIsWait = pawn.mindState.nextMoveOrderIsWait;
			pawn.mindState.nextMoveOrderIsWait = !pawn.mindState.nextMoveOrderIsWait;
			if (nextMoveOrderIsWait)
			{
				return new Job(RimWorld.JobDefOf.WaitWander)
				{
					expiryInterval = this.ticksBetweenWandersRange.RandomInRange
				};
			}


			IntVec3 c = this.getSwimmingCell(pawn);
			if (!c.IsValid)
			{
				pawn.mindState.nextMoveOrderIsWait = false;
				return null;
			}

			Job job = new Job(JobDefOf.TKKN_GoSwimming, c);
			pawn.Map.pawnDestinationReservationManager.Reserve(pawn, job, c);
			job.locomotionUrgency = this.locomotionUrgency;
			return job;
		}

		private IntVec3 getSwimmingCell(Pawn pawn)
		{
			IntVec3 wanderRoot = this.GetWanderRoot(pawn);
			IntVec3 c = RCellFinder.RandomWanderDestFor(pawn, wanderRoot, this.wanderRadius, this.wanderDestValidator, PawnUtility.ResolveMaxDanger(pawn, this.maxDanger));
			for (int i = 0; i < 20; i++)
			{
				IntVec3 c2 = c + GenAdj.AdjacentCellsAndInside[i];
				if (!c2.InBounds(pawn.Map))
				{
					return IntVec3.Invalid; 
				}
				if (!c2.Standable(pawn.Map))
				{
					return IntVec3.Invalid;
				}

				if (c2 is IntVec3 cell && cell.IsValid)
				{
					TerrainDef terrain = cell.GetTerrain(pawn.Map);
					if (terrain.HasTag("Water") && terrain.defName.Contains("Deep"))
					{
						return c2;

					}
				}
			}
			return IntVec3.Invalid;

		}

		protected override IntVec3 GetWanderRoot(Pawn pawn)
		{
			if (pawn.RaceProps.Humanlike)
			{
				Watcher watcher = pawn.Map.GetComponent<Watcher>();
				JobGiver_GoSwimming.swimmingSpots.Clear();

				for (int i = 0; i < watcher.swimmingCellsList.Count; i++)
				{
					IntVec3 position = watcher.swimmingCellsList[i];
					if (!position.IsForbidden(pawn) && pawn.CanReach(position, PathEndMode.Touch, Danger.None, false, TraverseMode.ByPawn))
					{
						JobGiver_GoSwimming.swimmingSpots.Add(position);
					}
				}
				if (JobGiver_GoSwimming.swimmingSpots.Count > 0)
				{
					return JobGiver_GoSwimming.swimmingSpots.RandomElement<IntVec3>();
				}
			}
			return IntVec3.Invalid;
		}
	}
}

namespace TKKN_NPS
{
	public class JobGiver_RelaxInSpring : ThinkNode_JobGiver
	{
		private float radius = 30f;

		protected override Job TryGiveJob(Pawn pawn)
		{
			Log.Warning("hi");
			if (!JoyUtility.EnjoyableOutsideNow(pawn, null))
			{
				return null;
			}

			Predicate<Thing> validator = delegate (Thing t)
			{
				if (t.def.defName == "TKKN_HotSpring" && t.AmbientTemperature < 26 && t.AmbientTemperature > 15)
				{
					return true;

				}
				if (t.def.defName == "TKKN_ColdSpring" && t.AmbientTemperature > 24)
				{
					return true;

				}
				return false;
			};
			Log.Warning("here");
			Thing hotSpring = GenClosest.ClosestThingReachable(pawn.GetLord().CurLordToil.FlagLoc, pawn.Map, ThingRequest.ForDef(TKKN_NPS.ThingDefOf.TKKN_HotSpring), PathEndMode.Touch, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), -1f, validator, null, 0, -1, false, RegionType.Set_Passable, false);
			if (hotSpring != null)
			{
				Thing spring = GenClosest.ClosestThingReachable(pawn.GetLord().CurLordToil.FlagLoc, pawn.Map, ThingRequest.ForDef(TKKN_NPS.ThingDefOf.TKKN_ColdSpring), PathEndMode.Touch, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), -1f, validator, null, 0, -1, false, RegionType.Set_Passable, false);
				Log.Warning("here2");
				if (spring != null)
				{
					return new Job(RimWorld.JobDefOf.GotoSafeTemperature, this.getSpringCell(spring));
				}
			}
			else
			{
				return new Job(RimWorld.JobDefOf.GotoSafeTemperature, this.getSpringCell(hotSpring));
			}
			return null;
		}

		private IntVec3 getSpringCell(Thing spring)
		{
			Log.Warning("sup");


			Predicate<IntVec3> validator = delegate (IntVec3 pos)
			{
				Log.Warning("sup2");
				if (spring.def.defName == "TKKN_HotSpring")
				{
					return pos.GetTerrain(spring.Map).defName == "TKKN_HotSpringsWater";
				}
				if (spring.def.defName == "TKKN_ColdSpring")
				{
					return pos.GetTerrain(spring.Map).defName == "TKKN_ColdSpringsWater";
				}
				return false;
			};
			Log.Warning("sup3");
			IntVec3 c = new IntVec3();
			CellFinder.TryFindRandomCellNear(spring.Position, spring.Map, 6, validator, out c);
			Log.Warning("sup4");
			return c;
		}
	}
}

