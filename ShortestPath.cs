using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace ConnPostSQL
{
    /// <summary>
    /// 完成使用PostgreSQL中提供的函数完成Dijkstra算法、Astar算法和Shooting-Star算法
    /// 可以对三个函数进行进一步的抽象，使用case句子进行判断，代码会更简练
    /// PostgreSQL中表名为小写，故代码中表名均为小写
    /// </summary>
    class ShortestPath
    {
        private DB postDB;
        public ShortestPath(DB outDB)
        {
            postDB = outDB;
        }

        #region pg 1.0的代码
        /*
        public void Dijkstra(string ftablename, string startpoint, string endpoint)
        {
            //进行计算前的准备工作，如为length字段赋值等在主程序中完成
            //在此处完成构建结果表的任务，在主程序中继续完善表，将结果表中的道路赋予几何实体
            string sqlstr, substr;
            string tablename;
            tablename = GetLayerName(ftablename) + "_dijkstra";
            substr = "select gid as id,source::integer,target::integer,length::double precision as cost from " + ftablename;
            sqlstr = "create table " + tablename + " as select * from shortest_path('" + substr + "'," + startpoint + "," + endpoint + ",false,false)";
            //需要检测结果表是否存在
            postDB.CheckCreateTable(tablename, sqlstr);
        }

        public void Astar(string ftablename, string startpoint, string endpoint)
        {
            string sqlstr, substr;
            string tablename;
            tablename = GetLayerName(ftablename) + "_astar";
            substr = "select gid as id,source::integer,target::integer,length::double precision as cost,x1,y1,x2,y2 from " + ftablename;
            sqlstr = "create table " + tablename + " as select * from shortest_path_astar('" + substr + "'," + startpoint + "," + endpoint + ",false,false)";
            postDB.CheckCreateTable(tablename, sqlstr);
        }

        public void ShootingStar(string ftablename, string startedge, string endedge)
        {
            string sqlstr, substr;
            string tablename;
            tablename = GetLayerName(ftablename) + "_shootingstar";
            substr = "select gid as id,source::integer,target::integer,length::double precision as cost,x1,y1,x2,y2,rule,to_cost from " + ftablename;
            sqlstr = "create table " + tablename + " as select * from shortest_path_shooting_star('" + substr + "'," + startedge + "," + endedge + ",false,false)";
            postDB.CheckCreateTable(tablename, sqlstr);
        }
         * */
        #endregion
        
#region 2.0 的新代码
        //shootingstar的意义不是很大，并且在2.0中没有提供此函数（迄今为止没看到1003），所以略去
        //将三个算法的函数进行合并，因为传递进来的参数除了算法名称之外其他都相同
        //重新抽象为一个函数，对算法名称进行switch case判断
        //新的算法结果里面只是列出来对应的顶点和边的编号，以及对应的耗费值，并没有几何实体，还需要结果
        //进行包装，才能正常显示在地图中，此部分工作其他函数不用，所以直接封装到此类中
        //20131004更新，将pgr提供的双向算法封装到此类中
        public void ComputePath(string algorithm, string ftablename, string startpoint, string endpoint)
        {
            string sqlstr, substr;
            string tablename,algorithmName;
            algorithmName = "pgr_";
            tablename = "";
            sqlstr = "";

            switch (algorithm)
            {
                case "Dijkstra":
                    algorithmName = algorithmName + algorithm.ToLower();
                    tablename = GetLayerName(ftablename) + "_dijkstra";
                    substr = "select gid as id,source::integer,target::integer,length::double precision as cost from " + ftablename;
                    sqlstr = "create table " + tablename + " as select * from " + algorithmName + "('" + substr + "'," + startpoint + "," + endpoint + ",false,false)";
                    break;
                case "Astar":
                    algorithmName = algorithmName + algorithm.ToLower();
                    tablename = GetLayerName(ftablename) + "_astar";
                    substr = "select gid as id,source::integer,target::integer,length::double precision as cost,x1,y1,x2,y2 from " + ftablename;
                    sqlstr = "create table " + tablename + " as select * from " + algorithmName + "('" + substr + "'," + startpoint + "," + endpoint + ",false,false)";
                    break;
                case "BdDijkstra":
                    algorithmName = algorithmName + algorithm.ToLower();
                    tablename = GetLayerName(ftablename) + "_bddijkstra";
                    substr = "select gid as id,source::integer,target::integer,length::double precision as cost from " + ftablename;
                    sqlstr = "create table " + tablename + " as select * from " + algorithmName + "('" + substr + "'," + startpoint + "," + endpoint + ",false,false)";
                    break;
                case "BdAstar":
                    algorithmName = algorithmName + algorithm.ToLower();
                    tablename = GetLayerName(ftablename) + "_bdastar";
                    substr = "select gid as id,source::integer,target::integer,length::double precision as cost,x1,y1,x2,y2 from " + ftablename;
                    sqlstr = "create table " + tablename + " as select * from " + algorithmName + "('" + substr + "'," + startpoint + "," + endpoint + ",false,false)";
                    break;
                default:
                    break;
            }            
            postDB.CheckCreateTable(tablename, sqlstr);
            UpdateShortestResult(ftablename, tablename);
        }

        //重载，需要传入另外两个变量
        public void ComputePath(string algorithm, string ftablename, string startpoint, string endpoint,bool directed,bool has_reverse_cost)
        {
            string sqlstr, substr;
            string tablename, algorithmName;
            algorithmName = "pgr_";
            tablename = "";
            sqlstr = "";

            switch (algorithm)
            {
                case "Dijkstra":
                    algorithmName = algorithmName + algorithm.ToLower();
                    tablename = GetLayerName(ftablename) + "_dijkstra";
                    substr = "select gid as id,source::integer,target::integer,length::double precision as cost from " + ftablename;
                    sqlstr = "create table " + tablename + " as select * from " + algorithmName + "('" + substr + "'," + startpoint + "," + endpoint + ","+ directed + "," + has_reverse_cost +")";
                    break;
                case "Astar":
                    algorithmName = algorithmName + algorithm.ToLower();
                    tablename = GetLayerName(ftablename) + "_astar";
                    substr = "select gid as id,source::integer,target::integer,length::double precision as cost,x1,y1,x2,y2 from " + ftablename;
                    sqlstr = "create table " + tablename + " as select * from " + algorithmName + "('" + substr + "'," + startpoint + "," + endpoint + "," + directed + "," + has_reverse_cost + ")";
                    break;
                case "BdDijkstra":
                    algorithmName = algorithmName + algorithm.ToLower();
                    tablename = GetLayerName(ftablename) + "_bddijkstra";
                    substr = "select gid as id,source::integer,target::integer,length::double precision as cost from " + ftablename;
                    sqlstr = "create table " + tablename + " as select * from " + algorithmName + "('" + substr + "'," + startpoint + "," + endpoint + "," + directed + "," + has_reverse_cost + ")";
                    break;
                case "BdAstar":
                    algorithmName = algorithmName + algorithm.ToLower();
                    tablename = GetLayerName(ftablename) + "_bdastar";
                    substr = "select gid as id,source::integer,target::integer,length::double precision as cost,x1,y1,x2,y2 from " + ftablename;
                    sqlstr = "create table " + tablename + " as select * from " + algorithmName + "('" + substr + "'," + startpoint + "," + endpoint + "," + directed + "," + has_reverse_cost + ")";
                    break;
                default:
                    break;
            }
            postDB.CheckCreateTable(tablename, sqlstr);
            UpdateShortestResult(ftablename, tablename);
        }
#endregion
        private string GetLayerName(string ftablename)
        {
            int index;
            string name;
            index = ftablename.LastIndexOf('_');
            name = ftablename.Substring(0, index);
            return name;
        }

        //为最短路径的结果添加几何实体，能够在地图上显示
        //20131004 为所有的计算结果统计总cost，写入到id2=-1的记录的cost字段中
        private void UpdateShortestResult(string ftablename, string tablename)
        {
            string geoColumn, sqlstr,tmpCost;
            DataTable tmpdt, tmpresult;
            double curCost, sumCost;
            bool isZero;
            geoColumn = "geom";
            curCost = 0.0;
            sumCost = 0.0;

            if (postDB.IsColumnExist(tablename, geoColumn))
            {
                sqlstr = "alter table " + tablename + " " + "drop column " + " " + geoColumn + " " + " cascade";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + geoColumn + " " + "geometry";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + geoColumn + " " + "geometry";
                postDB.ExecNonQuery(sqlstr);
            }
            //结果表中起始点或者终止点，对应的edge_id为-1，需要处理
            sqlstr = "select * from " + tablename + " order by seq asc";
            tmpdt = postDB.DoQueryEx(sqlstr);
            for (int i = 0; i <= tmpdt.Rows.Count - 1; i++)
            {
                //第二列对应的是边的id
                string edge_id = tmpdt.Rows[i]["id2"].ToString();
                tmpCost = tmpdt.Rows[i]["cost"].ToString();
                isZero = tmpCost.Equals("0") ? true : false;
                string tmpGeom;
                if (!edge_id.Equals("-1"))
                {
                    //双向算法中有些cost为0，此时需要查询并赋予值
                    sqlstr = "select st_astext(geom) as geomtext,length from " + " " + ftablename + " " + "where gid=" + edge_id;
                    tmpresult = postDB.DoQueryEx(sqlstr);
                    tmpGeom = tmpresult.Rows[0]["geomtext"].ToString();
                    //计算cost总和
                    if (isZero)
                    {
                        tmpCost = tmpresult.Rows[0]["length"].ToString();
                        curCost = Convert.ToDouble(tmpCost);
                        sqlstr = "update " + " " + tablename + " " + "set cost=" + curCost.ToString() + " " +"where id2=" + edge_id;
                        postDB.ExecNonQuery(sqlstr);
                    }
                    else
                    {
                        curCost = Convert.ToDouble(tmpdt.Rows[i]["cost"].ToString());
                    }
                    sumCost += curCost;
                    //这个地方涉及到一个问题，如果已经将newlines中的几何字段进行投影转变？此处处理方式需要改变
                    sqlstr = "update " + " " + tablename + " " + "set" + " " + geoColumn + "=" + "st_geomfromtext('" + tmpGeom + "', 4326) where id2=" + edge_id;
                    postDB.ExecNonQuery(sqlstr);
                }
                else
                {
                    sqlstr = "update" + " " + tablename + " " + "set cost=" + sumCost.ToString() + " "+ "where id2=" + edge_id;
                    postDB.ExecNonQuery(sqlstr);
                }
            }
        }
  

    }
}
