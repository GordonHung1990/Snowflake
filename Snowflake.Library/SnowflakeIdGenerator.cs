using System;

namespace Snowflake.Library
{
    /// <summary>
    /// 雪花ID產生器
    /// </summary>
    public class SnowflakeIdGenerator
    {
        /** 開始時間截 (2010-01-01 00:00:00) */
        private static string twepochString = "2010-01-01 00:00:00";
        private static long twepoch = (long)(DateTime.Parse(twepochString) - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;

        /** 機器id所佔的位數 */
        private static long workerIdBits = 5L;

        /** 數據標識id所佔的位數 */
        private static long datacenterIdBits = 5L;

        /** 支持的最大機器id，結果是31 (這個移位算法可以很快的計算出幾位二進制數所能表示的最大十進制數) */
        private static long maxWorkerId = -1L ^ (-1L << (int)workerIdBits);

        /** 支持的最大數據標識id，結果是31 */
        private static long maxDatacenterId = -1L ^ (-1L << (int)datacenterIdBits);

        /** 序列在id中佔的位數 */
        private static long sequenceBits = 12L;

        /** 機器ID向左移12位 */
        private static long workerIdShift = sequenceBits;

        /** 數據標識id向左移17位(12+5) */
        private static long datacenterIdShift = sequenceBits + workerIdBits;

        /** 時間截向左移22位(5+5+12) */
        private static long timestampLeftShift = sequenceBits + workerIdBits + datacenterIdBits;

        /** 生成序列的掩碼，這里為4095 (0b111111111111=0xfff=4095) */
        private static long sequenceMask = -1L ^ (-1L << (int)sequenceBits);

        /** 工作機器ID(0~31) */
        private long workerId;

        /** 數據中心ID(0~31) */
        private long datacenterId;

        /** 毫秒內序列(0~4095) */
        private long sequence = 0L;

        /** 上次生成ID的時間截 */
        private long lastTimestamp = -1L;

        private static readonly object lockObj = new object();

        /**
         * 構造函數
         * @param workerId 工作ID (0~31)
         * @param datacenterId 數據中心ID (0~31)
         */
        public SnowflakeIdGenerator(long workerId, long datacenterId)
        {
            if (workerId > maxWorkerId || workerId < 0)
            {
                throw new ArgumentOutOfRangeException(String.Format("worker Id can't be greater than %d or less than 0", maxWorkerId));
            }
            if (datacenterId > maxDatacenterId || datacenterId < 0)
            {
                throw new ArgumentOutOfRangeException(String.Format("datacenter Id can't be greater than %d or less than 0", maxDatacenterId));
            }
            this.workerId = workerId;
            this.datacenterId = datacenterId;
        }

        /**
         * 獲得下一個ID (該方法是線程安全的)
         * @return SnowflakeId
         */
        public long nextId()
        {
            lock (lockObj)
            {
                long timestamp = timeGen();

                //如果當前時間小於上一次ID生成的時間戳，說明系統時鐘回退過這個時候應當拋出異常
                if (timestamp < lastTimestamp)
                {
                    throw new Exception(
                    String.Format("Clock moved backwards. Refusing to generate id for %d milliseconds", lastTimestamp - timestamp));
                }

                //如果是同一時間生成的，則進行毫秒內序列
                if (lastTimestamp == timestamp)
                {
                    sequence = (sequence + 1) & sequenceMask;
                    //毫秒內序列溢出
                    if (sequence == 0)
                    {
                        //阻塞到下一個毫秒,獲得新的時間戳
                        timestamp = tilNextMillis(lastTimestamp);
                    }
                }
                //時間戳改變，毫秒內序列重置
                else
                {
                    sequence = 0L;
                }

                //上次生成ID的時間截
                lastTimestamp = timestamp;

                //移位並通過或運算拼到一起組成64位的ID
                return ((timestamp - twepoch) << (int)timestampLeftShift)
                    | (datacenterId << (int)datacenterIdShift)
                    | (workerId << (int)workerIdShift)
                    | sequence;
            }
        }

        /**
         * 阻塞到下一個毫秒，直到獲得新的時間戳
         * @param lastTimestamp 上次生成ID的時間截
         * @return 當前時間戳
         */
        protected long tilNextMillis(long lastTimestamp)
        {
            long timestamp = timeGen();
            while (timestamp <= lastTimestamp)
            {
                timestamp = timeGen();
            }
            return timestamp;
        }

        /**
         * 返回以毫秒為單位的當前時間
         * @return 當前時間(毫秒)
         */
        protected long timeGen()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }
    }
}
