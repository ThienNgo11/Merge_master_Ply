using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace DatdevUlts.Ults
{
    public static class ExtensionMethods
    {
        // Hàm kiểm tra xem layer có nằm trong mask không
        public static bool Contains(this LayerMask mask, int layer)
        {
            return (mask.value & (1 << layer)) != 0;
        }
    }
    
    public static class Math2DUlts
    {
        /// <summary>
        /// Trả về euler để vectors up (hoặc right) hướng đến. 
        /// </summary>
        /// <param name="forward">Hướng nhìn mong muốn</param>
        /// <param name="direction">Y = up; X = right</param>
        /// <returns></returns>
        public static Vector3 LookEuler(Vector2 forward, EDirection direction = EDirection.Y)
        {
            if (direction == EDirection.Y)
            {
                float angle = Vector3.Angle(Vector3.up, forward.normalized);
                if (forward.x >= 0)
                {
                    angle = -angle;
                }

                return new Vector3(0, 0, angle);
            }
            else
            {
                float angle = Vector3.Angle(Vector3.right, forward.normalized);
                if (forward.y <= 0)
                {
                    angle = -angle;
                }

                return new Vector3(0, 0, angle);
            }
        }

        /// <summary>
        /// Trả về rotation để vectors up (hoặc right) hướng đến. 
        /// </summary>
        /// <param name="forward">Hướng nhìn mong muốn</param>
        /// <param name="direction">Y = up; X = right</param>
        /// <returns></returns>
        public static Quaternion LookAt(Vector2 forward, EDirection direction = EDirection.Y)
        {
            return Quaternion.Euler(LookEuler(forward, direction));
        }

        /// <summary>
        /// Trả về rotation mà from sẽ quay dần dần để direction hướng đến to
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="delta"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static Quaternion RotateTowards(Quaternion from, Vector2 to, float delta,
            EDirection direction = EDirection.Y)
        {
            return Quaternion.RotateTowards(from, LookAt(to, direction), delta);
        }

        /// <summary>
        /// Trả về một vector có hướng hợp với góc truyền vào theo đường tròn, độ dài là một.
        /// </summary>
        public static Vector2 GetDirectionByAngle(float angle)
        {
            return new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        }

        /// <summary>
        /// Trả về một toạ độ có hướng hợp với góc truyền vào theo đường tròn, cách trung điểm radius.
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="point">Trung điểm</param>
        /// <param name="radius">Bán kính</param>
        /// <returns></returns>
        public static Vector2 GetPositionByAngle(float angle, Vector2 point, float radius)
        {
            return GetDirectionByAngle(angle) * radius + point;
        }

        /// <summary>
        /// Trả về một vị trí ngẫu nhiên trong vùng tròn
        /// </summary>
        /// <param name="point">Trung điểm</param>
        /// <param name="radius">Bán kính</param>
        public static Vector2 GetRandomPositionCircle(Vector2 point, float radius)
        {
            return GetPositionByAngle(UnityEngine.Random.Range(-180f, 180f), point, radius);
        }

        /// <summary>
        /// Trả về một vị trí ngẫu nhiên trong vùng tròn nhỏ đến to
        /// </summary>
        /// <param name="point">Trung điểm</param>
        /// <param name="radiusFrom">Bán kính nhỏ</param>
        /// <param name="radiusTo">Bán kính to</param>
        /// <returns></returns>
        public static Vector2 GetRandomPositionCircle(Vector2 point, float radiusFrom, float radiusTo)
        {
            return GetPositionByAngle(UnityEngine.Random.Range(-180f, 180f), point,
                UnityEngine.Random.Range(radiusFrom, radiusTo));
        }

        public static Vector2 GetRandomPositionCircle(Vector2 point, float angleFrom, float angleTo, float radius)
        {
            return GetPositionByAngle(UnityEngine.Random.Range(angleFrom, angleTo), point, radius);
        }

        public static Vector2 GetRandomPositionCircle(Vector2 point, float angleFrom, float angleTo, float radiusFrom,
            float radiusTo)
        {
            return GetPositionByAngle(UnityEngine.Random.Range(angleFrom, angleTo), point,
                UnityEngine.Random.Range(radiusFrom, radiusTo));
        }

        /// <summary>
        /// Trả về trọng tâm các điểm
        /// </summary>
        public static Vector2 GetCenter(params Vector2[] points)
        {
            Vector2 center = new Vector2();
            for (int i = 0; i < points.Length; i++)
            {
                center += points[i];
            }

            return center / points.Length;
        }

        /// <summary>
        /// Trả về Vector ở xa vị trí đã cho nhất
        /// </summary>
        /// <returns></returns>
        public static Vector2 MaxDistance(Vector2 point, params Vector2[] points)
        {
            Vector2 max = points[0];
            for (int i = 0; i < points.Length; i++)
            {
                if ((points[i] - point).magnitude > ((max - point).magnitude))
                {
                    max = points[i];
                }
            }

            return max;
        }

        /// <summary>
        /// Trả về Transform ở xa vị trí đã cho nhất
        /// </summary>
        /// <returns></returns>
        public static Transform MaxDistance(Transform point, params MonoBehaviour[] points)
        {
            Transform max = points[0].transform;
            for (int i = 0; i < points.Length; i++)
            {
                if ((points[i].transform.position - point.position).magnitude >
                    ((max.position - point.position).magnitude))
                {
                    max = points[i].transform;
                }
            }

            return max;
        }

        /// <summary>
        /// Trả về Vector ở gần vị trí đã cho nhất
        /// </summary>
        /// <returns></returns>
        public static Vector2 MinDistance(Vector2 point, params Vector2[] points)
        {
            Vector2 min = points[0];
            for (int i = 0; i < points.Length; i++)
            {
                if ((points[i] - point).magnitude < ((min - point).magnitude))
                {
                    min = points[i];
                }
            }

            return min;
        }

        /// <summary>
        /// Trả về Transform ở gần vị trí đã cho nhất
        /// </summary>
        /// <returns></returns>
        public static MonoBehaviour MinDistance(Transform point, params MonoBehaviour[] points)
        {
            MonoBehaviour min = points[0];
            for (int i = 0; i < points.Length; i++)
            {
                if ((points[i].transform.position - point.position).magnitude <
                    ((min.transform.position - point.position).magnitude))
                {
                    min = points[i];
                }
            }

            return min;
        }

        /// <summary>
        /// Trả về điểm mà là trung điểm của đa giác gần vị trí đã cho nhất
        /// </summary>
        /// <returns></returns>
        public static Vector2 MinDistanceMidPointOfPolygon(Vector2 point, params Vector2[] points)
        {
            List<Vector2> list = new List<Vector2>();
            for (int i = 0; i < points.Length - 1; i++)
            {
                list.Add((points[i] + points[i + 1]) / 2);
            }

            return MinDistance(point, list.ToArray());
        }

        /// <summary>
        /// Trả về điểm mà là trung điểm của đa giác xa vị trí đã cho nhất
        /// </summary>
        /// <returns></returns>
        public static Vector2 MaxDistanceMidPointOfPolygon(Vector2 point, params Vector2[] points)
        {
            List<Vector2> list = new List<Vector2>();
            for (int i = 0; i < points.Length - 1; i++)
            {
                list.Add((points[i] + points[i + 1]) / 2);
            }

            return MaxDistance(point, list.ToArray());
        }

        /// <summary>
        /// Trả về hướng mà hướng đã cho xoay thêm góc
        /// </summary>
        /// <param name="forward"></param>
        /// <param name="_angle"></param>
        /// <returns></returns>
        public static Vector2 AddAngle(Vector2 forward, float _angle)
        {
            float angle = Vector2.Angle(Vector2.right, forward.normalized);
            if (forward.y <= 0)
            {
                angle = -angle;
            }

            angle += _angle;
            return new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * forward.magnitude;
        }

        /// <summary>
        /// Trả về điểm cắt gần nhất của toạ độ với các vòng tròn cắt nhau
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="radius"></param>
        /// <param name="points"></param>
        /// <returns></returns>
        public static Vector2 MinCircleCut(Vector2 pos, float radius, params Vector2[] points)
        {
            List<Vector2> p = new List<Vector2>();
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector2 td = (points[i + 1] + points[i]) / 2;
                float goc = Mathf.Acos((td - points[i]).magnitude / radius) * Mathf.Rad2Deg;
                p.Add(AddAngle((td - points[i]), goc).normalized * radius + points[i]);
                p.Add(AddAngle((td - points[i]), -goc).normalized * radius + points[i]);
            }

            return MinDistance(pos, p.ToArray());
        }

        /// <summary>
        /// Trả về một vector random -1,-1; 1,1
        /// </summary>
        /// <returns></returns>
        public static Vector2 Random()
        {
            return new Vector2(UnityEngine.Random.Range(-1, 1), UnityEngine.Random.Range(-1, 1));
        }

        public static Vector2 Random(float minx, float miny, float maxx, float maxy)
        {
            return new Vector2(UnityEngine.Random.Range(minx, maxx), UnityEngine.Random.Range(miny, maxy));
        }

        public static IEnumerator Shrink(Transform transform, float velocity, float gravityScale = -1)
        {
            var oldPos = transform.position;
            var oldVelo = velocity;
            while (true)
            {
                transform.position = new Vector3(transform.position.x, transform.position.y + velocity * Time.deltaTime,
                    transform.position.z);
                velocity += -9.81f * gravityScale * Time.deltaTime;
                if (oldVelo < 0 && velocity > -oldVelo)
                {
                    transform.position = oldPos;
                    yield break;
                }

                if (oldVelo > 0 && velocity < -oldVelo)
                {
                    transform.position = oldPos;
                    yield break;
                }

                yield return null;
            }
        }
    }

    public static class Move2DUlts
    {
        /// <summary>
        /// Nhảy với tốc độ phương X ban đầu
        /// </summary>
        /// <param name="input"></param>
        /// <param name="setting"></param>
        /// <param name="speedX"></param>
        /// <param name="onUpdate"></param>
        /// <param name="onComplete"></param>
        /// <param name="onStart"></param>
        /// <returns></returns>
        public static IEnumerator JumpWithSpeed(MoveInput input, MoveSetting setting, float speedX,
            Action<CurrentMoveInfo> onUpdate = null, Action<CurrentMoveInfo> onComplete = null,
            Action<CurrentMoveInfo> onStart = null)
        {
            var timeX = SpeedX2Time(input, speedX, setting.scaleGravity);
            return JumpWithTime(input, setting, timeX, onUpdate, onComplete, onStart);
        }


        /// <summary>
        /// Từ speed theo chiều X, tính ra thời gian di chuyển ném xiên
        /// </summary>
        /// <param name="input"></param>
        /// <param name="speedX"></param>
        /// <param name="scaleGravity"></param>
        /// <returns></returns>
        public static float SpeedX2Time(MoveInput input, float speedX, float scaleGravity)
        {
            float distanceX = Mathf.Abs(input.endPos.x - input.startPos.x);
            float timeX = distanceX / speedX;

            if (timeX < 0)
            {
                timeX = -timeX;
            }

            return Mathf.Max(timeX,
                GetTimeOfJumpWithMaxHeight(input, Mathf.Max(input.startPos.y, input.endPos.y), scaleGravity));
        }


        /// <summary>
        /// Nhảy sao cho điểm cao nhất là Max Height
        /// </summary>
        /// <param name="input"></param>
        /// <param name="setting"></param>
        /// <param name="maxHeight"></param>
        /// <param name="onUpdate"></param>
        /// <param name="onComplete"></param>
        /// <param name="onStart"></param>
        /// <returns></returns>
        public static IEnumerator JumpWithMaxHeight(MoveInput input, MoveSetting setting, float maxHeight,
            Action<CurrentMoveInfo> onUpdate = null, Action<CurrentMoveInfo> onComplete = null,
            Action<CurrentMoveInfo> onStart = null)
        {
            var timeX = GetTimeOfJumpWithMaxHeight(input, maxHeight, setting.scaleGravity);

            return JumpWithTime(input, setting, timeX, onUpdate, onComplete, onStart);
        }


        /// <summary>
        /// Tính thời gian để sao cho nhảy mà điểm cao nhất là maxHeight 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="maxHeight"></param>
        /// <param name="scaleGravity"></param>
        /// <returns></returns>
        public static float GetTimeOfJumpWithMaxHeight(MoveInput input, float maxHeight, float scaleGravity = 1f)
        {
            // Tính toán thời gian di chuyển
            float gravity = scaleGravity * 9.81f;
            // Tính toán vận tốc ban đầu theo chiều Y dựa trên công thức ném xiên
            //float velocityY = (endPos.y - startPos.y - 0.5f * -9.81f * scaleG * timeX * timeX) / timeX;
            float velocityY = Mathf.Sqrt(2 * (maxHeight - input.startPos.y) * gravity);

            float x1 = 0, x2 = 0;
            var a = 0.5f * -gravity;
            var b = velocityY;
            var c = input.startPos.y - input.endPos.y;
            if (a == 0)
            {
                if (b == 0)
                {
                    if (c == 0)
                    {
                    }
                }

                x1 = -c / b;
            }
            else
            {
                var delta = b * b - 4 * a * c;
                if (delta == 0)
                {
                    x1 = -b / 2 / a;
                }
                else
                {
                    if (delta < 0)
                    {
                    }
                    else
                    {
                        x1 = (-b - Mathf.Sqrt(delta)) / 2 / a;
                        x2 = (-b + Mathf.Sqrt(delta)) / 2 / a;
                    }
                }
            }

            var timeX = (x1 > x2 ? x1 : x2);
            return timeX;
        }


        /// <param name="input"></param>
        /// <param name="setting"></param>
        /// <param name="angle">Góc so với mặt đất (luôn dương)</param>
        /// <param name="onUpdate"></param>
        /// <param name="onComplete"></param>
        /// <param name="onStart"></param>
        /// <returns></returns>
        public static IEnumerator JumpToAngle(MoveInput input, MoveSetting setting, float angle,
            Action<CurrentMoveInfo> onUpdate = null, Action<CurrentMoveInfo> onComplete = null,
            Action<CurrentMoveInfo> onStart = null)
        {
            var timeX = Angle2Time(input, angle, setting.scaleGravity);
            return JumpWithTime(input, setting, timeX, onUpdate, onComplete, onStart);
        }


        /// <summary>
        /// Từ góc ban đầu tính ra thời gian tiêu tốn trong ném xiên
        /// </summary>
        /// <param name="input"></param>
        /// <param name="scaleGravity"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static float Angle2Time(MoveInput input, float angle, float scaleGravity = 1f)
        {
            // Tính toán thời gian di chuyển
            float distanceX = Mathf.Abs(input.endPos.x - input.startPos.x);
            var a = input.endPos.y - input.startPos.y;
            var b = 0.5f * -9.81f * scaleGravity;
            var c = distanceX;
            var t = Mathf.Tan(angle * Mathf.Deg2Rad);
            var timeX = Mathf.Sqrt((a - t * c) / b);
            return timeX;
        }


        /// <summary>
        /// Hàm cuối trong chuyển động ném xiên, các hàm khác sẽ tính ra thời gian và gọi hàm này
        /// </summary>
        /// <param name="input"></param>
        /// <param name="setting"></param>
        /// <param name="time"></param>
        /// <param name="onUpdate"></param>
        /// <param name="onComplete"></param>
        /// <param name="onStart"></param>
        /// <returns></returns>
        public static IEnumerator JumpWithTime(MoveInput input, MoveSetting setting, float time,
            Action<CurrentMoveInfo> onUpdate = null, Action<CurrentMoveInfo> onComplete = null,
            Action<CurrentMoveInfo> onStart = null)
        {
            // Tính toán thời gian di chuyển
            float distanceX = Mathf.Abs(input.endPos.x - input.startPos.x);


            // Tính toán vận tốc ban đầu theo chiều Y dựa trên công thức ném xiên
            Vector2 initVelocity;
            initVelocity.x = distanceX / time;
            if (input.endPos.x < input.startPos.x)
            {
                initVelocity.x = -initVelocity.x;
            }

            initVelocity.y = (input.endPos.y - input.startPos.y - 0.5f * -9.81f * setting.scaleGravity * time * time) /
                             time;

            // Set-up và start
            var gravity = -9.81f * setting.scaleGravity;
            CurrentMoveInfo currentMoveInfo = new CurrentMoveInfo
            {
                currentPosition = input.startPos,
                currentTime = 0,
                currentVelocity = initVelocity
            };
            onStart?.Invoke(currentMoveInfo);

            while (currentMoveInfo.currentTime < time)
            {
                onUpdate?.Invoke(currentMoveInfo);

                yield return null;

                currentMoveInfo.currentTime +=
                    (setting.ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime) *
                    setting.timeScale;
                var currentTime = currentMoveInfo.currentTime;
                currentMoveInfo.currentPosition = new Vector3()
                {
                    x = input.startPos.x + initVelocity.x * currentTime,
                    y = input.startPos.y + initVelocity.y * currentTime + gravity * currentTime * currentTime / 2f
                };
                currentMoveInfo.currentVelocity.y = initVelocity.y + gravity * currentTime;
            }

            currentMoveInfo.currentPosition = input.endPos;
            onUpdate?.Invoke(currentMoveInfo);

            onComplete?.Invoke(currentMoveInfo);
        }


        /// <summary>
        /// Kéo một vật về điểm kết thúc trong thời gian
        /// </summary>
        /// <param name="input"></param>
        /// <param name="initVelo"></param>
        /// <param name="timeTaken"></param>
        /// <param name="onUpdate"></param>
        /// <param name="onComplete"></param>
        /// <param name="onStart"></param>
        /// <returns></returns>
        public static IEnumerator Pull(MoveInput input, Vector2 initVelo, float timeTaken,
            Action<CurrentMoveInfo> onUpdate = null, Action<CurrentMoveInfo> onComplete = null,
            Action<CurrentMoveInfo> onStart = null)
        {
            var acc = new Vector3
            {
                x = 2 * (input.endPos.x - input.startPos.x - initVelo.x * timeTaken) / timeTaken / timeTaken,
                y = 2 * (input.endPos.y - input.startPos.y - initVelo.y * timeTaken) / timeTaken / timeTaken
            };


            var currentMoveInfo = new CurrentMoveInfo()
            {
                currentPosition = input.startPos,
                currentTime = 0,
                currentVelocity = initVelo
            };
            onStart?.Invoke(currentMoveInfo);

            while (currentMoveInfo.currentTime < timeTaken)
            {
                onUpdate?.Invoke(currentMoveInfo);

                yield return null;

                currentMoveInfo.currentTime += Time.deltaTime;
                currentMoveInfo.currentPosition += currentMoveInfo.currentVelocity * Time.deltaTime;
                currentMoveInfo.currentVelocity.x = initVelo.x + acc.x * currentMoveInfo.currentTime;
                currentMoveInfo.currentVelocity.y = initVelo.y + acc.y * currentMoveInfo.currentTime;
            }

            currentMoveInfo.currentPosition = input.endPos;
            onUpdate?.Invoke(currentMoveInfo);

            onComplete?.Invoke(currentMoveInfo);
        }
    }

    public static class RandomUlts
    {
        private static System.Random random = new System.Random();

        /// <summary>
        /// Random để tìm một khoảng, sau đó trả về giá trị trong khoảng đó
        /// </summary>
        /// <param name="intervals"></param>
        /// <returns></returns>
        public static float RandomRanges(params Range[] intervals)
        {
            var range = Random.Range(0, intervals.Length);
            return Random.Range(intervals[range].min, intervals[range].max);
        }

        /// <summary>
        /// Random để tìm một khoảng, sau đó trả về giá trị trong khoảng đó [min, max]
        /// </summary>
        /// <param name="intervals"></param>
        /// <returns></returns>
        public static int RandomRangesInt(params Range[] intervals)
        {
            var range = Random.Range(0, intervals.Length);
            return Random.Range((int)intervals[range].min, (int)intervals[range].max + 1);
        }

        public static int GetRandomIndexByRate(float[] rate, params int[] exclude)
        {
            float sum = 0;
            for (int i = 0; i < rate.Length; i++)
            {
                if (exclude.Contains(i))
                {
                    continue;
                }

                sum += rate[i];
            }

            if (sum <= 0)
            {
                return -1;
            }

            float num = Random.Range(0, sum);
            for (int i = 0; i < rate.Length; i++)
            {
                if (exclude.Contains(i))
                {
                    continue;
                }

                if (num <= rate[i])
                {
                    return i;
                }

                num -= rate[i];
            }

            Debug.LogError("Error");
            return -1;
        }

        public static T GetRandomByRate<T>(T[] list, float[] rate, params int[] exclude)
        {
            return list[GetRandomIndexByRate(rate, exclude)];
        }

        public static T GetRandom<T>(T[] list, params int[] excludeIndex)
        {
            float[] rate = new float[list.Length];
            for (int i = 0; i < rate.Length; i++)
            {
                rate[i] = 1;
            }

            return list[GetRandomIndexByRate(rate, excludeIndex)];
        }

        public static T GetRandom<T>(this ICollection<T> collection)
        {
            return collection.ElementAt(Range(0, collection.Count - 1));
        }

        /// Lấy cái nhất, arg1: Cái đang so sánh với arg2 (Cái đang là est), true thì est = arg1
        public static T GetEst<T>(this ICollection<T> collection, Func<T, T, bool> compare)
        {
            if (collection == null || collection.Count == 0)
            {
                throw new ArgumentException("The collection cannot be null or empty.");
            }

            using (var enumerator = collection.GetEnumerator())
            {
                enumerator.MoveNext();
                T min = enumerator.Current;

                foreach (var item in collection)
                {
                    if (compare(item, min))
                    {
                        min = item;
                    }
                }

                return min;
            }
        }

        public static int GetRandom(Range rangeInt)
        {
            return Random.Range((int)rangeInt.min, (int)rangeInt.max + 1);
        }

        public static List<T> GetRandomElements<T>(ICollection<T> items, int count)
        {
            var weights = new List<float>();

            for (int i = 0; i < items.Count; i++)
            {
                weights.Add(1);
            }

            return GetRandomElements(items, weights, count);
        }

        public static List<T> GetRandomElements<T>(ICollection<T> elements, List<float> weights, int count)
        {
            if (elements == null || weights == null || elements.Count == 0 || weights.Count == 0 || elements.Count != weights.Count)
            {
                return null;
            }

            var oldelements = elements;
            elements = new List<T>();
            elements.AddRange(oldelements);

            var oldweights = weights;
            weights = new List<float>();
            weights.AddRange(oldweights);

            List<T> selectedElements = new List<T>();

            // Tính tổng trọng số
            float totalWeight = weights.Sum();

            for (int i = 0; i < count; i++)
            {
                float randomValue = (float)random.NextDouble() * totalWeight;
                float cumulativeWeight = 0f;

                // Chọn phần tử dựa trên trọng số
                for (int j = 0; j < elements.Count; j++)
                {
                    cumulativeWeight += weights[j];
                    if (randomValue <= cumulativeWeight)
                    {
                        selectedElements.Add(((List<T>)elements)[j]);
                        totalWeight -= weights[j];
                        ((List<T>)elements).RemoveAt(j);
                        weights.RemoveAt(j);
                        break;
                    }
                }
            }

            return selectedElements;
        }

        /// Hàm tạo số ngẫu nhiên kiểu int trong khoảng [min, max]
        public static int Range(int min, int max)
        {
            if (min > max)
            {
                (min, max) = (max, min);
            }

            // Sử dụng NextBytes để tạo ngẫu nhiên các byte, sau đó chuyển thành kiểu long
            byte[] buffer = new byte[4];
            random.NextBytes(buffer);
            int value = BitConverter.ToInt32(buffer, 0);

            // Đảm bảo giá trị nằm trong khoảng [min, max]
            return (Math.Abs(value % (max - min + 1)) + min);
        }

        /// Hàm tạo số ngẫu nhiên kiểu long trong khoảng [min, max]
        public static long Range(long min, long max)
        {
            if (min > max)
            {
                (min, max) = (max, min);
            }

            // Sử dụng NextBytes để tạo ngẫu nhiên các byte, sau đó chuyển thành kiểu long
            byte[] buffer = new byte[8];
            random.NextBytes(buffer);
            long longValue = BitConverter.ToInt64(buffer, 0);

            // Đảm bảo giá trị nằm trong khoảng [min, max]
            return (Math.Abs(longValue % (max - min + 1)) + min);
        }

        /// Hàm tạo số ngẫu nhiên kiểu float trong khoảng [min, max]
        public static float Range(float min, float max)
        {
            if (min > max)
            {
                (min, max) = (max, min);
            }

            // Tạo số ngẫu nhiên kiểu float trong khoảng [0, 1) và scale nó để nằm trong khoảng [min, max]
            return (float)(random.NextDouble() * (max - min) + min);
        }

        /// Hàm tạo số ngẫu nhiên kiểu double trong khoảng [min, max]
        public static double Range(double min, double max)
        {
            if (min > max)
            {
                (min, max) = (max, min);
            }

            // Tạo số ngẫu nhiên kiểu double trong khoảng [0, 1) và scale nó để nằm trong khoảng [min, max]
            return random.NextDouble() * (max - min) + min;
        }
    }

    public static class CanvasPositioningExtensions
    {
        public static Vector3 WorldToCanvasPosition(this RectTransform canvas, Vector3 worldPosition,
            Camera camera = null)
        {
            if (camera == null)
            {
                camera = Camera.main;
            }

            // ReSharper disable once PossibleNullReferenceException
            var viewportPosition = camera.WorldToViewportPoint(worldPosition);
            return canvas.ViewportToCanvasPosition(viewportPosition);
        }

        public static Vector3 ScreenToCanvasPosition(this RectTransform canvas, Vector3 screenPosition)
        {
            var viewportPosition = new Vector3(screenPosition.x / Screen.width,
                screenPosition.y / Screen.height,
                0);
            return canvas.ViewportToCanvasPosition(viewportPosition);
        }

        public static Vector3 ViewportToCanvasPosition(this RectTransform rectTransform, Vector3 viewportPosition)
        {
            var centerBasedViewPortPosition = viewportPosition - new Vector3(0.5f, 0.5f, 0);
            var scale = rectTransform.rect.size;
            return Vector3.Scale(centerBasedViewPortPosition, scale);
        }

        /// <summary>
        /// Chuyển đổi vị trí local của một phần tử trong Canvas sang vị trí viewport.
        /// </summary>
        /// <param name="rectTransform">RectTransform chứa phần tử.</param>
        /// <param name="localPos">Vị trí local cần chuyển đổi.</param>
        /// <returns>Vị trí trong không gian viewport.</returns>
        public static Vector3 LocalPositionToViewport(this RectTransform rectTransform, Vector3 localPos)
        {
            var scale = rectTransform.rect.size;
            var centerBasedLocalPos = Vector3.Scale(localPos, new Vector3(1 / scale.x, 1 / scale.y, 1));
            return centerBasedLocalPos + new Vector3(0.5f, 0.5f, 0);
        }
        
        public static Vector3 WorldPositionToViewport(this RectTransform rectTransform, Vector3 worldPos)
        {
            return rectTransform.LocalPositionToViewport(rectTransform.InverseTransformPoint(worldPos));
        }

        private static Vector3[] corners = new Vector3[4];

        /// <summary>
        /// Returns a random world position that lies inside the given RectTransform.
        /// </summary>
        /// <param name="rectTransform">The RectTransform to find a position inside of.</param>
        /// <returns>A Vector3 representing a random world position inside the RectTransform.</returns>
        public static Vector3 GetRandomWorldPositionInside(this RectTransform rectTransform)
        {
            // Get the corners of the RectTransform in world space
            rectTransform.GetWorldCorners(corners);

            // Get random point in normalized coordinates
            float randomX = Random.Range(0.0f, 1.0f);
            float randomY = Random.Range(0.0f, 1.0f);

            // Interpolate to find a point inside the RectTransform
            Vector3 positionInside = corners[0]
                                     + randomX * (corners[3] - corners[0])
                                     + randomY * (corners[1] - corners[0]);

            return positionInside;
        }
    }

    public static class DateTimeUls
    {
        public static bool IsDiffDay(DateTime dateTime1, DateTime dateTime2)
        {
            if (dateTime2.DayOfYear != dateTime1.DayOfYear)
            {
                return true;
            }

            if (dateTime2.Year != dateTime1.Year)
            {
                return true;
            }

            return false;
        }

        public static DateTime RandomRange(DateTime min, DateTime max)
        {
            return new DateTime(RandomUlts.Range(min.Ticks, max.Ticks));
        }

        /// Phương thức để kiểm tra xem x có phải là ngày hôm qua của y không
        public static bool IsYesterday(DateTime yesterday, DateTime now)
        {
            return IsLastAmountDay(yesterday, now, 1);
        }

        /// Phương thức để kiểm tra xem x có phải là ngày hôm qua của y không
        public static bool IsLastAmountDay(DateTime lastDay, DateTime now, int amountDay)
        {
            // Lấy ngày hôm qua
            DateTime y = now.AddDays(-amountDay);

            // So sánh x với ngày hôm qua của y
            return lastDay.Date == y.Date;
        }

        // Hàm để tìm ngày chủ nhật tiếp theo
        public static DateTime GetNextSunday(DateTime inputDate)
        {
            // Tính toán số ngày cần thêm để đến chủ nhật tiếp theo
            int daysUntilNextSunday = ((int)DayOfWeek.Sunday - (int)inputDate.DayOfWeek + 7) % 7;

            // Thêm số ngày đó vào ngày đầu vào
            DateTime nextSunday = inputDate.AddDays(daysUntilNextSunday);

            return new DateTime(nextSunday.Year, nextSunday.Month, nextSunday.Day, 23, 59, 59);
        }

        // Hàm để tính tuần trong năm
        public static int GetWeekNumber(DateTime inputDate)
        {
            // Sử dụng phương pháp ISO-8601 để tính tuần
            // Tuần bắt đầu từ thứ 2 và kết thúc vào chủ nhật

            // Tính toán ngày đầu tiên của năm
            DateTime firstDayOfYear = new DateTime(inputDate.Year, 1, 1);

            // Tính toán ngày bắt đầu của tuần đầu tiên của năm
            DateTime firstMondayOfYear = firstDayOfYear.AddDays(1 - (int)firstDayOfYear.DayOfWeek);

            // Tính toán số tuần bằng cách tính số ngày kể từ ngày bắt đầu của năm đến ngày hiện tại,
            // sau đó chia cho 7 (số ngày trong một tuần)
            int weekNumber = (inputDate.Subtract(firstMondayOfYear).Days / 7) + 1;

            return weekNumber;
        }
    }

    public static class TransformUlts
    {
        /// <summary>
        /// Ví dụ: neo vị trí lên canvas rồi đổi sang world space để lấy biên giới màn hình
        /// </summary>
        /// <param name="posFrom"></param>
        /// <param name="camera"></param>
        /// <param name="depth">Khoảng cách của vị trí mới so với mặt phẳng cắt đi qua vị trí camera song song với near plane của camera</param>
        /// <returns></returns>
        public static Vector3 FromCameraToWorld(Vector3 posFrom, Camera camera, float depth)
        {
            var screenPoint = camera.WorldToScreenPoint(posFrom);
            screenPoint.z = depth;
            return camera.ScreenToWorldPoint(screenPoint);
        }

        /// <summary>
        /// FD: Tìm các component của thằng con trực tiếp với trans
        /// </summary>
        /// <param name="trans"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T[] GetComponentsInChildrenFD<T>(this Transform trans)
        {
            List<T> list = new List<T>();
            for (int i = 0; i < trans.childCount; i++)
            {
                var component = trans.GetChild(i).GetComponent<T>();
                if (component != null)
                {
                    list.Add(component);
                }
            }

            return list.ToArray();
        }

        /// <summary>
        /// Tìm Transform tên aName là con của aParent
        /// </summary>
        /// <param name="aParent"></param>
        /// <param name="aName"></param>
        /// <returns></returns>
        public static Transform FindChildInParent(this Transform aParent, string aName)
        {
            aName = aName.ToLower();
            foreach (Transform child in aParent)
            {
                if (child.name.ToLower() == aName)
                    return child;
                var result = child.FindChildInParent(aName);
                if (result != null)
                    return result;
            }

            return null;
        }

        public static GameObject[] FindDeepChildsWithStartName(this Transform aParent, string startName)
        {
            startName = startName.ToLower();
            List<GameObject> result = new List<GameObject>();
            foreach (Transform child in aParent)
            {
                if (child.name.ToLower().StartsWith(startName))
                {
                    result.Add(child.gameObject);
                }
                else
                {
                    var childResult = child.FindDeepChildsWithStartName(startName);
                    result.AddRange(childResult);
                }
            }

            return result.ToArray();
        }

        public static Transform FindDeepChildWithStartName(this Transform aParent, string startName)
        {
            startName = startName.ToLower();
            foreach (Transform child in aParent)
            {
                if (child.name.ToLower().StartsWith(startName))
                    return child;
                var result = child.FindDeepChildWithStartName(startName);
                if (result != null)
                    return result;
            }

            return null;
        }

        public static GameObject[] FindChildsSameDeep(this Transform trans, string startName, bool includeInactive)
        {
            Transform result = FindDeepChildWithStartName(trans, startName);
            List<GameObject> list = new List<GameObject>();
            if (result != null)
            {
                Transform aParent = result.parent;
                for (int i = 0; i < aParent.childCount; i++)
                {
                    var obj = aParent.GetChild(i);
                    if ((!includeInactive || obj.gameObject.activeSelf) && obj.name.StartsWith(startName))
                    {
                        list.Add(obj.gameObject);
                    }
                }
            }

            return list.ToArray();
        }

        public static int GetChildCount(this Transform trans, bool includeInactive)
        {
            if (includeInactive)
            {
                return trans.childCount;
            }
            else
            {
                int count = 0;
                for (int i = 0; i < trans.childCount; ++i)
                {
                    if (trans.GetChild(i).gameObject.activeSelf)
                    {
                        ++count;
                    }
                }

                return count;
            }
        }

        public static RectTransform RectTransform(this Component component)
        {
            return (RectTransform)component.transform;
        }
    }

    public static class RendererUtils
    {
        public static void ConvertAllSpriteToImage(this Transform parent)
        {
            SpriteRenderer[] listSpr = parent.GetComponentsInChildren<SpriteRenderer>();
            for (int i = 0; i < listSpr.Length; i++)
            {
                SpriteRenderer spriteRenderer = listSpr[i];
                GameObject gameObject = listSpr[i].gameObject;
                RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
                Image image = gameObject.AddComponent<Image>();
                Vector2 size = spriteRenderer.sprite.rect.size;
                rectTransform.sizeDelta = size / spriteRenderer.sprite.pixelsPerUnit;
                image.sprite = spriteRenderer.sprite;
                Object.Destroy(spriteRenderer);
            }
        }
    }

    public static class CoroutineUtils
    {
        public static Coroutine DelayedCall(this MonoBehaviour monoBehaviour, float timeDelay, Action action, bool ignoreTimeScale = false)
        {
            return monoBehaviour.StartCoroutine(Action());

            IEnumerator Action()
            {
                if (ignoreTimeScale)
                {
                    yield return new WaitForSecondsRealtime(timeDelay);
                }
                else
                {
                    yield return new WaitForSeconds(timeDelay);
                }

                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public static Tween DODelayedCall(this GameObject gameObject, float timeDelay, Action action, bool ignoreTimeScale = false)
        {
            if (!gameObject.activeInHierarchy)
            {
                return null;
            }
            Tween tween = null;
            tween = DOVirtual.DelayedCall(timeDelay, () =>
            {
                if (!gameObject || !gameObject.activeInHierarchy)
                {
                    CoroutineRunner.Instance.RemoveDoDelay(tween);
                }
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                // ReSharper disable once AccessToModifiedClosure
                CoroutineRunner.Instance.RemoveDoDelay(tween);
            }, ignoreTimeScale);
            CoroutineRunner.Instance.AddDoDelay(gameObject, tween);
            return tween;
        }

        public static void StopAllDODelayCall(this GameObject gameObject)
        {
            CoroutineRunner.Instance.RemoveDoDelay(gameObject);
        }
    }

    public static class IListExtensions
    {
        /// <summary>
        /// Shuffle the list in place using the Fisher-Yates method.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        /// <summary>
        /// Return a random item from the list.
        /// Sampling with replacement.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static T RandomItem<T>(this IList<T> list)
        {
            if (list.Count == 0)
                throw new IndexOutOfRangeException("Cannot select a random item from an empty list");
            return list[Random.Range(0, list.Count)];
        }

        /// <summary>
        /// Removes a random item from the list, returning that item.
        /// Sampling without replacement.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static T RemoveRandom<T>(this IList<T> list)
        {
            if (list.Count == 0)
                throw new IndexOutOfRangeException("Cannot remove a random item from an empty list");
            int index = Random.Range(0, list.Count);
            T item = list[index];
            list.RemoveAt(index);
            return item;
        }

        /// <summary>
        /// Change index of item from the list.
        /// Sampling without replacement.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="oldIndex"></param>
        /// <param name="newIndex"></param>
        /// <returns></returns>
        public static void Move<T>(this List<T> list, int oldIndex, int newIndex)
        {
            T item = list[oldIndex];
            list.RemoveAt(oldIndex);
            list.Insert(newIndex, item);
        }

        public static void AddRange<T>(this ICollection<T> target, IEnumerable<T> source)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            foreach (var element in source)
                target.Add(element);
        }
    }

    public static class CollectionExtensions
    {
        public static void NullAllItems(this Array collection)
        {
            for (int i = 0; i < collection.Length; i++)
            {
                collection.SetValue(null, i);
            }
        }
    }

    public static class PhysicsExtensions
    {
        public static void SortRaycastHits(Vector3 origin, RaycastHit[] hits)
        {
            Array.Sort(hits, (hit1, hit2) =>
            {
                if (!hit1.transform && !hit2.transform)
                {
                    return 0;
                }

                if (!hit1.transform)
                {
                    return 1;
                }

                if (!hit2.transform)
                {
                    return -1;
                }

                if ((hit1.point - origin).magnitude > (hit2.point - origin).magnitude)
                {
                    return 1;
                }

                if ((hit1.point - origin).magnitude < (hit2.point - origin).magnitude)
                {
                    return -1;
                }

                return 0;
            });

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform)
                {
                    Debug.Log($"{i}: {hits[i].transform.name}", hits[i].transform.gameObject);
                }
            }
        }
    }

    public static class LogExtensions
    {
        public static void LogError(this object message, Object context = null, Color color = default,
            bool bold = true)
        {
            Debug.LogError(message, context);
        }

        public static void LogWarning(this object message, Object context = null, Color color = default,
            bool bold = true)
        {
            Debug.LogWarning(message, context);
        }

        public static void Log(this object message, Object context = null, Color color = default,
            bool bold = true)
        {
            Debug.Log(message, context);
        }

        public static void LogException(this Exception exception, Object context = null, Color color = default,
            bool bold = true)
        {
            Debug.LogException(new Exception(exception.Message, exception), context);
        }
    }

    public static class EnumExtensions
    {
        /// <summary>
        /// Converts an Enum value to the specified Enum type.
        /// </summary>
        /// <param name="value">The Enum value to convert.</param>
        /// <typeparam name="T">The target Enum type.</typeparam>
        /// <returns>The converted Enum value of type T.</returns>
        public static T ToEnum<T>(this Enum value) where T : Enum
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Value cannot be null.");
            }

            if (!Enum.IsDefined(typeof(T), value.ToString()))
            {
                throw new ArgumentException($"The value '{value}' is not defined in the enum type '{typeof(T).Name}'.");
            }
            
            return (T)Enum.Parse(typeof(T), value.ToString(), true);
        }
        
        /// <summary>
        /// Checks if the Enum value can be converted to the specified Enum type.
        /// </summary>
        /// <param name="value">The Enum value to check.</param>
        /// <typeparam name="T">The target Enum type.</typeparam>
        /// <returns>True if the value can be converted to type T; otherwise, false.</returns>
        public static bool CanConvertToEnum<T>(this Enum value) where T : Enum
        {
            if (value == null)
            {
                return false;
            }

            // Kiểm tra xem giá trị có tồn tại trong loại enum mục tiêu hay không
            return Enum.IsDefined(typeof(T), value.ToString());
        }
    }


    [Serializable]
    public struct Range
    {
        public float min;
        public float max;

        public Range(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }

    public enum EDirection
    {
        X,
        Y
    }

    [Serializable]
    public struct MoveInput
    {
        public Vector3 startPos;
        public Vector3 endPos;

        public MoveInput(Vector3 startPos, Vector3 endPos)
        {
            this.startPos = startPos;
            this.endPos = endPos;
        }
    }

    [Serializable]
    public struct CurrentMoveInfo
    {
        public Vector3 currentPosition;
        public Vector3 currentVelocity;
        public float currentTime;
    }

    [Serializable]
    public struct MoveSetting
    {
        public float timeScale;
        public bool ignoreTimeScale;
        public float scaleGravity;

        public static MoveSetting Default =>
            new MoveSetting()
            {
                ignoreTimeScale = false,
                timeScale = 1,
                scaleGravity = 1,
            };
    }

    [Serializable]
    public class InvertRectTransform
    {
        [SerializeField] private RectTransform _rectTfCurrent;
        [SerializeField] private RectTransform _rectTfWantInvert;

        public RectTransform RectTfCurrent => _rectTfCurrent;

        public void Refresh()
        {
            _rectTfCurrent.transform.position = _rectTfWantInvert.transform.position;
        }
    }

    public class SortingLayerAtt : PropertyAttribute
    {
    }
}