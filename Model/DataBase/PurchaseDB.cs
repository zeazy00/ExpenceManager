﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;

namespace Model.DataBase
{
	public static class PurchaseDB
	{
		public static DateTime? GetLastPurchaseDate()
		{
			using var db = new PurchaseDBContext();

			var items = db.Purchases;

			if (items.Count() > 0) return items.OrderBy(x => x.Date).Last().Date;

			return null;
		}

		public static IEnumerable<DateTime> GetAvailableDates()
		{
			using var db = new PurchaseDBContext();

			return db.Purchases.Select(x => x.Date);
		}

		public static IEnumerable<GoodType> GetAllGoodTypes()
		{
			using var db = new PurchaseDBContext();

			return DistinctCollections(
				db.Purchases.Select(x => x.GetPurchaseItems().Select(x => x.Type).Distinct()),
				null			//There's no rull just skip item
				);
		}

		public static IEnumerable<PurchaseItem> GetTypeAtDateRange(GoodType type, DateTime initialDate, DateTime? finalDate)
		{
			using var db = new PurchaseDBContext();

			ApproximateDateToLower(ref initialDate);

			if (finalDate.HasValue)
			{
				ApproximateDateToUpper(ref finalDate);

				return DistinctCollections(
										db.Purchases.Where(x => x.Date >= initialDate && x.Date <= finalDate.Value)
													.Select(x => x.GetPurchaseItems().Where(x => x.Type == type)),
										(item1, item2) => item1.Amount += item2.Amount		//Sum amount of equal items
										);
			}
			else
			{
				return DistinctCollections(
										db.Purchases.Where(x => x.Date.Year == initialDate.Year &&
															x.Date.Month == initialDate.Month &&
															x.Date.Day == initialDate.Day)
													.Select(x => x.GetPurchaseItems().Where(x => x.Type == type)),
										(item1, item2) => item1.Amount += item2.Amount      //Sum amount of equal items
										);

			}

		}

		/// <summary>
		/// Distinct the same items in all <paramref name="purchases"/>
		/// </summary>
		/// <param name="purchases">Purchases</param>
		/// <param name="DistinctionRule">Rule to distinct items</param>
		/// <returns>New joined purchase items</returns>
		private static IEnumerable<T> DistinctCollections<T>(IEnumerable<IEnumerable<T>> purchases, Action<T, T> DistinctionRule)
													where T : IEquatable<T>
		{
			var result = new List<T>();

			foreach (var purchase in purchases)
			{
				foreach (var item in purchase)
				{
					if (result.Contains(item))
					{
						//result[result.IndexOf(item)].Amount += item.Amount;
						DistinctionRule?.Invoke(result[result.IndexOf(item)], item);
					}
					else
					{
						result.Add(item);
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Approximating date to the very begginning
		/// </summary>
		/// <param name="initialDate"></param>
		private static void ApproximateDateToLower(ref DateTime initialDate)
		{
			initialDate = initialDate.AddHours(-initialDate.Hour)
									.AddMinutes(-initialDate.Minute)
									.AddSeconds(-initialDate.Second);
		}

		private static void ApproximateDateToUpper(ref DateTime? finalDate)
		{
			var h = 23 - finalDate.Value.Hour;
			var m = 59 - finalDate.Value.Minute;
			var s = 59 - finalDate.Value.Second;

			finalDate = finalDate.Value.AddHours(h).AddMinutes(m).AddSeconds(s);
		}
	}
}