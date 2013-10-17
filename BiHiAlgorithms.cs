using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Collections;

namespace ConnPostSQL
{
    class BiHiAlgorithms
    {
        private DB postDB;
        public BiHiAlgorithms(DB outDB)
        {
            postDB = outDB;
        }

        //如果结果是通过几个表合并得到的，那么保留最后一个id2=-1的记录，用于统计cost
        public void ComputePath(string algorithm, string ftablename, string startpoint, string endpoint)
        {
            string sqlstr, substr;
            string sk, ek;
            string tablename, algorithmName,resultTable;
            DataTable tmpdt;

            algorithmName = "pgr_";
            tablename = "";
            substr = "";
            sqlstr = "";

            //设置用到的道路图层
            SetRoadLength();
            tablename = GetLayerName(ftablename) + "_vertex_adjaction";
            sqlstr = "select kind from " + tablename + " where vertex_id=" + startpoint;
            tmpdt = postDB.DoQueryEx(sqlstr);
            sk = tmpdt.Rows[0]["kind"].ToString();
            sqlstr = "select kind from " + tablename + " where vertex_id=" + endpoint;
            tmpdt = postDB.DoQueryEx(sqlstr);
            ek = tmpdt.Rows[0]["kind"].ToString();

            resultTable = GetLayerName(ftablename) + "_hierarchy_"+ algorithm.ToLower();
            algorithmName = algorithmName + algorithm.ToLower();

            if (sk.Equals(ek))
            {
                //处在同一层
                if (sk.Equals("1"))
                {
                    //位于第一层
                    tablename = "level" + sk + "_newlines";
                    switch (algorithm)
                    {
                        case "BdDijkstra":
                            substr = "select gid as id,source::integer,target::integer,length::double precision as cost from " + tablename;
                            break;
                        case "BdAstar":
                            substr = "select gid as id,source::integer,target::integer,length::double precision as cost,x1,y1,x2,y2 from " + tablename;
                            break;
                        default:
                            break;
                    }
                    sqlstr = "create table " + resultTable + " as select * from " + algorithmName + "('" + substr + "'," + startpoint + "," + endpoint + ",false,false)";
                    //需要检测结果表是否存在
                    postDB.CheckCreateTable(resultTable, sqlstr);
                    UpdateShortestResult(ftablename, resultTable);
                }
                else
                {
                    //位于第二层
                    //表示两个结点所处的区域编号
                    string sSubNum, eSubNum;

                    tablename = GetLayerName(ftablename) + "_vertex_adjaction";
                    sqlstr = "select subgraphnum from " + tablename + " where vertex_id=" + startpoint;
                    tmpdt = postDB.DoQueryEx(sqlstr);
                    sSubNum = tmpdt.Rows[0]["subgraphnum"].ToString();
                    sqlstr = "select subgraphnum from " + tablename + " where vertex_id=" + endpoint;
                    tmpdt = postDB.DoQueryEx(sqlstr);
                    eSubNum = tmpdt.Rows[0]["subgraphnum"].ToString();

                    if (sSubNum.Equals(eSubNum))
                    {
                        //位于相同的区内
                        string subTable;

                        subTable = GetLayerName(ftablename) + "_subtable_" + eSubNum;                        
                        switch (algorithm)
                        {
                            case "BdDijkstra":
                                sqlstr = "create table " + subTable + " " + "as select a.gid as id,a.source::integer,a.target::integer,a.length::double precision as cost from " + ftablename +
                                    " a," + "level1_polygon b where st_contains(st_buffer(st_transform(b.the_geom,2345),5),st_transform(a.geom,2345))=true and b.subnum=" + sSubNum;
                                break;
                            case "BdAstar":
                                sqlstr = "create table " + subTable + " as select a.gid as id,a.source::integer,a.target::integer,a.length::double precision as cost,x1,y1,x2,y2 from " + ftablename +
                                    " a," + "level1_polygon b where st_contains(st_buffer(st_transform(b.the_geom,2345),5),st_transform(a.geom,2345))=true and b.subnum=" + sSubNum;
                                break;
                            default:
                                break;
                        }
                        postDB.CheckCreateTable(subTable, sqlstr);
                        substr = "select * from " + subTable;
                        sqlstr = "create table " + resultTable + " as select * from " + algorithmName + "('" + substr + "'," + startpoint + "," + endpoint + ",false,false)";
                        //需要检测结果表是否存在
                        postDB.CheckCreateTable(resultTable, sqlstr);
                        UpdateShortestResult(ftablename, resultTable);
                    }
                    else
                    {
                        //位于不同的区内
                        string sSubTable, eSubTable, entrypointTable, ePoints, eResultTable, midResultTable;
                        DataTable tmpEntryPoint;
                        string transformPoint1, transformPoint2;

                        sSubTable = GetLayerName(ftablename) + "_subtable_" + sSubNum;
                        //将落入对应编号多边形的线提取出来
                        switch (algorithm)
                        {
                            case "BdDijkstra":
                                sqlstr = "create table " + sSubTable + " " + "as select a.gid as id,a.source::integer,a.target::integer,a.length::double precision as cost from " + ftablename +
                                    " a," + "level1_polygon b where st_contains(st_buffer(st_transform(b.the_geom,2345),5),st_transform(a.geom,2345))=true and b.subnum=" + sSubNum;
                                break;
                            case "BdAstar":
                                sqlstr = "create table " + sSubTable + " as select a.gid as id,a.source::integer,a.target::integer,a.length::double precision as cost,x1,y1,x2,y2 from " + ftablename +
                                    " a," + "level1_polygon b where st_contains(st_buffer(st_transform(b.the_geom,2345),5),st_transform(a.geom,2345))=true and b.subnum=" + sSubNum;
                                break;
                            default:
                                break;
                        }
                        postDB.CheckCreateTable(sSubTable, sqlstr);


                        eSubTable = GetLayerName(ftablename) + "_subtable_" + eSubNum;
                        switch (algorithm)
                        {
                            case "BdDijkstra":
                                sqlstr = "create table " + eSubTable + " " + "as select a.gid as id,a.source::integer,a.target::integer,a.length::double precision as cost from " + ftablename +
                                    " a," + "level1_polygon b where st_contains(st_buffer(st_transform(b.the_geom,2345),5),st_transform(a.geom,2345))=true and b.subnum=" + eSubNum;
                                break;
                            case "BdAstar":
                                sqlstr = "create table " + eSubTable + " as select a.gid as id,a.source::integer,a.target::integer,a.length::double precision as cost,x1,y1,x2,y2 from " + ftablename +
                                    " a," + "level1_polygon b where st_contains(st_buffer(st_transform(b.the_geom,2345),5),st_transform(a.geom,2345))=true and b.subnum=" + eSubNum;
                                break;
                            default:
                                break;
                        }
                        postDB.CheckCreateTable(eSubTable, sqlstr);

                        //分别找出两个区的入口点，计算区域所有入口点到两个点的距离加和，取最小值对应的点为选取的入口点
                        entrypointTable = GetLayerName(ftablename) + "_entrypoint_subgraph";
                        sqlstr = "select entrypoint from " + entrypointTable + " where subnum=" + sSubNum;
                        tmpEntryPoint = postDB.DoQueryEx(sqlstr);
                        ePoints = tmpEntryPoint.Rows[0]["entrypoint"].ToString();
                        transformPoint1 = GetEntryPoint(ePoints, startpoint, endpoint, ftablename);

                        substr = "select * from " + sSubTable;
                        sqlstr = "create table " + resultTable + " as select * from " + algorithmName + "('" + substr + "'," + startpoint + "," + transformPoint1 + ",false,false)";
                        //需要检测结果表是否存在
                        postDB.CheckCreateTable(resultTable, sqlstr);
                        UpdateShortestResult(ftablename, resultTable);

                        sqlstr = "select entrypoint from " + entrypointTable + " where subnum=" + eSubNum;
                        tmpEntryPoint = postDB.DoQueryEx(sqlstr);
                        ePoints = tmpEntryPoint.Rows[0]["entrypoint"].ToString();
                        transformPoint2 = GetEntryPoint(ePoints, startpoint, endpoint, ftablename);

                        substr = "select * from " + eSubTable;
                        eResultTable = GetLayerName(ftablename) + "_path_end";
                        sqlstr = "create table " + eResultTable + " as select * from " + algorithmName + "('" + substr + "'," + transformPoint2 + "," + endpoint + ",false,false)";
                        //将结果存储到临时表中
                        postDB.CheckCreateTable(eResultTable, sqlstr);
                        UpdateShortestResult(ftablename, eResultTable);

                        //查找两个区域入口点之间最短路径在level1上进行
                        tablename = "level1_newlines";
                        midResultTable = GetLayerName(ftablename) + "_path_mid";
                        switch (algorithm)
                        {
                            case "BdDijkstra":
                                substr = "select gid as id,source::integer,target::integer,length::double precision as cost from " + tablename;
                                break;
                            case "BdAstar":
                                substr = "select gid as id,source::integer,target::integer,length::double precision as cost,x1,y1,x2,y2 from " + tablename;
                                break;
                            default:
                                break;
                        } 
                        sqlstr = "create table " + midResultTable + " as select * from " + algorithmName + "('" + substr + "'," + transformPoint1 + "," + transformPoint2 + ",false,false)";
                        //需要检测结果表是否存在
                        postDB.CheckCreateTable(midResultTable, sqlstr);
                        UpdateShortestResult(tablename, midResultTable);

                        //下一步需要将两个临时表中的结果更新到最终结果表中
                        //结果正确后将两个临时表删除
                        sqlstr = "insert into " + " " + resultTable + " " + "select * from " + midResultTable + " " + "where edge_id <> -1";
                        //sqlstr = "insert into " + " " + resultTable + " " + "select * from " + midResultTable;
                        postDB.ExecNonQuery(sqlstr);
                        //保留最后一个边为-1的记录，用于记录cost统计结果
                        //sqlstr = "insert into " + " " + resultTable + " " + "select * from " + eSubTable + " " + "where edge_id <> -1";
                        sqlstr = "insert into " + " " + resultTable + " " + "select * from " + eSubTable;
                        postDB.ExecNonQuery(sqlstr);
                        ComputeTotalCost(resultTable,endpoint);
                        //此处也可以考虑将中间的转换点也插入到记录中，作为结果的一种可视化表示
                        //sqlstr = "insert into " + " " + resultTable + " " + "select * from " + midResultTable;
                        //postDB.ExecNonQuery(sqlstr);
                        //sqlstr = "insert into " + " " + resultTable + " " + "select * from " + eSubTable;
                        //postDB.ExecNonQuery(sqlstr);
                    }

                }
            }
            else
            {
                //处在不同层
                //需要判断哪个点出于底层
                string sSubTable, eSubTable, entrypointTable, ePoints, eResultTable, midResultTable;
                DataTable tmpEntryPoint;
                string transformPoint;
                int startkind, endkind;
                string eSubNum, sSubNum;

                startkind = int.Parse(sk);
                endkind = int.Parse(ek);

                tablename = GetLayerName(ftablename) + "_vertex_adjaction";
                sqlstr = "select subgraphnum from " + tablename + " where vertex_id=" + startpoint;
                tmpdt = postDB.DoQueryEx(sqlstr);
                sSubNum = tmpdt.Rows[0]["subgraphnum"].ToString();
                sqlstr = "select subgraphnum from " + tablename + " where vertex_id=" + endpoint;
                tmpdt = postDB.DoQueryEx(sqlstr);
                eSubNum = tmpdt.Rows[0]["subgraphnum"].ToString();

                if (startkind > endkind)
                {
                    //起始点位于底层网络
                    sSubTable = GetLayerName(ftablename) + "_subtable_" + sSubNum;
                    switch (algorithm)
                    {
                        case "BdDijkstra":
                            sqlstr = "create table " + sSubTable + " " + "as select a.gid as id,a.source::integer,a.target::integer,a.length::double precision as cost from " + ftablename +
                                " a," + "level1_polygon b where st_contains(st_buffer(st_transform(b.the_geom,2345),5),st_transform(a.geom,2345))=true and b.subnum=" + sSubNum;
                            break;
                        case "BdAstar":
                            sqlstr = "create table " + sSubTable + " as select a.gid as id,a.source::integer,a.target::integer,a.length::double precision as cost,x1,y1,x2,y2 from " + ftablename +
                                " a," + "level1_polygon b where st_contains(st_buffer(st_transform(b.the_geom,2345),5),st_transform(a.geom,2345))=true and b.subnum=" + sSubNum;
                            break;
                        default:
                            break;
                    }
                    postDB.CheckCreateTable(sSubTable, sqlstr);

                    //分别入口点，计算区域所有入口点到两个点的距离加和，取最小值对应的点为选取的入口点
                    entrypointTable = GetLayerName(ftablename) + "_entrypoint_subgraph";
                    sqlstr = "select entrypoint from " + entrypointTable + " where subnum=" + sSubNum;
                    tmpEntryPoint = postDB.DoQueryEx(sqlstr);
                    ePoints = tmpEntryPoint.Rows[0]["entrypoint"].ToString();
                    transformPoint = GetEntryPoint(ePoints, startpoint, endpoint, ftablename);

                    //计算最短路径
                    substr = "select * from " + sSubTable;
                    sqlstr = "create table " + resultTable + " as select * from " + algorithmName + "('" + substr + "'," + startpoint + "," + transformPoint + ",false,false)";
                    //需要检测结果表是否存在
                    postDB.CheckCreateTable(resultTable, sqlstr);
                    UpdateShortestResult(ftablename, resultTable);

                    //查找两个区域入口点之间最短路径在level1上进行
                    tablename = "level1_newlines";
                    midResultTable = GetLayerName(ftablename) + "_path_mid";
                    switch (algorithm)
                    {
                        case "BdDijkstra":
                            substr = "select gid as id,source::integer,target::integer,length::double precision as cost from " + tablename;
                            break;
                        case "BdAstar":
                            substr = "select gid as id,source::integer,target::integer,length::double precision as cost,x1,y1,x2,y2 from " + tablename;
                            break;
                        default:
                            break;
                    } 
                    sqlstr = "create table " + midResultTable + " as select * from " + algorithmName + "('" + substr + "'," + transformPoint + "," + endpoint + ",false,false)";
                    //需要检测结果表是否存在
                    postDB.CheckCreateTable(midResultTable, sqlstr);
                    UpdateShortestResult(tablename, midResultTable);

                    //下一步需要将两个临时表中的结果更新到最终结果表中
                    //结果正确后将两个临时表删除
                    //sqlstr = "insert into " + " " + resultTable + " " + "select * from " + midResultTable + " " + "where edge_id <> -1";
                    sqlstr = "insert into " + " " + resultTable + " " + "select * from " + midResultTable;
                    postDB.ExecNonQuery(sqlstr);
                    ComputeTotalCost(resultTable,endpoint);

                }
                else
                {
                    //终止点位于底层网络
                    eSubTable = GetLayerName(ftablename) + "_subtable_" + eSubNum;
                    switch (algorithm)
                    {
                        case "BdDijkstra":
                            sqlstr = "create table " + eSubTable + " " + "as select a.gid as id,a.source::integer,a.target::integer,a.length::double precision as cost from " + ftablename +
                                " a," + "level1_polygon b where st_contains(st_buffer(st_transform(b.the_geom,2345),5),st_transform(a.geom,2345))=true and b.subnum=" + eSubNum;
                            break;
                        case "BdAstar":
                            sqlstr = "create table " + eSubTable + " as select a.gid as id,a.source::integer,a.target::integer,a.length::double precision as cost,x1,y1,x2,y2 from " + ftablename +
                                " a," + "level1_polygon b where st_contains(st_buffer(st_transform(b.the_geom,2345),5),st_transform(a.geom,2345))=true and b.subnum=" + eSubNum;
                            break;
                        default:
                            break;
                    }
                    postDB.CheckCreateTable(eSubTable, sqlstr);

                    //分别入口点，计算区域所有入口点到两个点的距离加和，取最小值对应的点为选取的入口点
                    entrypointTable = GetLayerName(ftablename) + "_entrypoint_subgraph";
                    sqlstr = "select entrypoint from " + entrypointTable + " where subnum=" + eSubNum;
                    tmpEntryPoint = postDB.DoQueryEx(sqlstr);
                    ePoints = tmpEntryPoint.Rows[0]["entrypoint"].ToString();
                    transformPoint = GetEntryPoint(ePoints, startpoint, endpoint, ftablename);

                    //查找两个区域入口点之间最短路径在level1上进行
                    tablename = "level1_newlines";
                    //substr = "select gid as id,source::integer,target::integer,length::double precision as cost from " + tablename;
                    switch (algorithm)
                    {
                        case "BdDijkstra":
                            substr = "select gid as id,source::integer,target::integer,length::double precision as cost from " + tablename;
                            break;
                        case "BdAstar":
                            substr = "select gid as id,source::integer,target::integer,length::double precision as cost,x1,y1,x2,y2 from " + tablename;
                            break;
                        default:
                            break;
                    }
                    sqlstr = "create table " + resultTable + " as select * from " + algorithmName + "('" + substr + "'," + startpoint + "," + transformPoint + ",false,false)";
                    //需要检测结果表是否存在
                    postDB.CheckCreateTable(resultTable, sqlstr);
                    UpdateShortestResult(tablename, resultTable);

                    //计算最短路径
                    eResultTable = GetLayerName(ftablename) + "_path_end";
                    substr = "select * from " + eSubTable;
                    sqlstr = "create table " + eResultTable + " as select * from " + algorithmName + "('" + substr + "'," + transformPoint + "," + endpoint + ",false,false)";
                    //需要检测结果表是否存在
                    postDB.CheckCreateTable(eResultTable, sqlstr);
                    UpdateShortestResult(ftablename, eResultTable);

                    //下一步需要将两个临时表中的结果更新到最终结果表中
                    //结果正确后将两个临时表删除
                    //sqlstr = "insert into " + " " + resultTable + " " + "select * from " + eResultTable + " " + "where edge_id <> -1";
                    sqlstr = "insert into " + " " + resultTable + " " + "select * from " + eResultTable;
                    postDB.ExecNonQuery(sqlstr);
                    ComputeTotalCost(resultTable,endpoint);
                }
            }

        }

        private string GetLayerName(string ftablename)
        {
            int index;
            string name;
            index = ftablename.LastIndexOf('_');
            name = ftablename.Substring(0, index);
            return name;
        }

        /// <summary>
        /// 返回最合适的入口点
        /// </summary>
        /// <param name="ePoints">备选的入口点字符串</param>
        /// <param name="startpoint">起始点</param>
        /// <param name="endpoint">终止点</param>
        /// <param name="ftablename">图层名称</param>
        /// <returns></returns>
        private string GetEntryPoint(string ePoints, string startpoint, string endpoint, string ftablename)
        {
            //从一个区域的入口点中选择合适的一个，距离和最小
            string[] points;
            string tablename, sqlstr, tmpPoint;
            double xs, ys, xt, yt, xe, ye;
            double distST, distTE;
            Hashtable ht;
            DataTable tmpdt;
            string[] keyPoints;
            double[] valuePoints;

            tablename = GetLayerName(ftablename) + "_vertex_adjaction";
            //使用标准函数st_x和st_y
            sqlstr = "select st_x(st_transform(geom,2345)) as x,st_y(st_transform(geom,2345)) as y from " + tablename + " where vertex_id=" + startpoint;
            tmpdt = postDB.DoQueryEx(sqlstr);
            xs = double.Parse(tmpdt.Rows[0]["x"].ToString());
            ys = double.Parse(tmpdt.Rows[0]["y"].ToString());
            sqlstr = "select st_x(st_transform(geom,2345)) as x,st_y(st_transform(geom,2345)) as y from " + tablename + " where vertex_id=" + endpoint;
            tmpdt = postDB.DoQueryEx(sqlstr);
            xe = double.Parse(tmpdt.Rows[0]["x"].ToString());
            ye = double.Parse(tmpdt.Rows[0]["y"].ToString());
            points = ePoints.Split('_');
            ht = new Hashtable();
            for (int i = 0; i < points.Length; i++)
            {
                ht.Clear();
                tmpPoint = points[i];
                sqlstr = "select st_x(st_transform(geom,2345)) as x,st_y(st_transform(geom,2345)) as y from " + tablename + " where vertex_id=" + tmpPoint;
                tmpdt = postDB.DoQueryEx(sqlstr);
                xt = double.Parse(tmpdt.Rows[0]["x"].ToString());
                yt = double.Parse(tmpdt.Rows[0]["y"].ToString());
                distST = Math.Sqrt((xt - xs) * (xt - xs) + (yt - ys) * (yt - ys));
                distTE = Math.Sqrt((xt - xe) * (xt - xe) + (yt - ye) * (yt - ye));
                ht.Add(tmpPoint, distST + distTE);
            }
            keyPoints = new string[ht.Count];
            valuePoints = new double[ht.Count];
            ht.Keys.CopyTo(keyPoints, 0);
            ht.Values.CopyTo(valuePoints, 0);
            Array.Sort(valuePoints, keyPoints);

            return keyPoints[0];
        }

        /// <summary>
        /// 为使用的图层添加length字段
        /// </summary>
        private void SetRoadLength()
        {
            //设置道路的length
            string tablename, sqlstr, colName, geoClm, projectsrid, columnName;
            //为道路图层的geometry添加index
            string indexName;

            #region level1
            tablename = "level1_newlines";
            geoClm = "geom";
            colName = "length";
            projectsrid = "2345";
            if (postDB.IsColumnExist(tablename, colName))
            {
                sqlstr = "update " + tablename + " " + "set " + colName + "=st_length(st_transform(" + geoClm + "," + projectsrid + "))";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + colName + " " + "double precision";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + colName + "=st_length(st_transform(" + geoClm + "," + projectsrid + "))";
                postDB.ExecNonQuery(sqlstr);
            }

            columnName = "x1";
            if (postDB.IsColumnExist(tablename, columnName))
            {
                sqlstr = "update " + tablename + " " + "set " + columnName + "=st_x(startpoint(" + geoClm + "))";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + columnName + " " + "double precision";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + columnName + "=st_x(startpoint(" + geoClm + "))";
                postDB.ExecNonQuery(sqlstr);
            }
            columnName = "y1";
            if (postDB.IsColumnExist(tablename, columnName))
            {
                sqlstr = "update " + tablename + " " + "set " + columnName + "=st_y(startpoint(" + geoClm + "))";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + columnName + " " + "double precision";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + columnName + "=st_y(startpoint(" + geoClm + "))";
                postDB.ExecNonQuery(sqlstr);
            }
            columnName = "x2";
            if (postDB.IsColumnExist(tablename, columnName))
            {
                sqlstr = "update " + tablename + " " + "set " + columnName + "=st_x(endpoint(" + geoClm + "))";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + columnName + " " + "double precision";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + columnName + "=st_x(endpoint(" + geoClm + "))";
                postDB.ExecNonQuery(sqlstr);
            }
            columnName = "y2";
            if (postDB.IsColumnExist(tablename, columnName))
            {
                sqlstr = "update " + tablename + " " + "set " + columnName + "=st_y(endpoint(" + geoClm + "))";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + columnName + " " + "double precision";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + columnName + "=st_y(endpoint(" + geoClm + "))";
                postDB.ExecNonQuery(sqlstr);
            }

            indexName = "level1_index_geom";
            if (postDB.IsIndexExist(tablename, geoClm))
            {
                sqlstr = "drop index " + " " + indexName;
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "create index" + " " + indexName + " " + "on" + " " + tablename + " " + "using gist(" + geoClm + ")";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "create index" + " " + indexName + " " + "on" + " " + tablename + " " + "using gist(" + geoClm + ")";
                postDB.ExecNonQuery(sqlstr);
            }
            #endregion

            #region level2
            tablename = "level2_newlines";
            if (postDB.IsColumnExist(tablename, colName))
            {
                sqlstr = "update " + tablename + " " + "set " + colName + "=st_length(st_transform(" + geoClm + "," + projectsrid + "))";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + colName + " " + "double precision";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + colName + "=st_length(st_transform(" + geoClm + "," + projectsrid + "))";
                postDB.ExecNonQuery(sqlstr);
            }

            columnName = "x1";
            if (postDB.IsColumnExist(tablename, columnName))
            {
                sqlstr = "update " + tablename + " " + "set " + columnName + "=st_x(startpoint(" + geoClm + "))";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + columnName + " " + "double precision";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + columnName + "=st_x(startpoint(" + geoClm + "))";
                postDB.ExecNonQuery(sqlstr);
            }
            columnName = "y1";
            if (postDB.IsColumnExist(tablename, columnName))
            {
                sqlstr = "update " + tablename + " " + "set " + columnName + "=st_y(startpoint(" + geoClm + "))";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + columnName + " " + "double precision";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + columnName + "=st_y(startpoint(" + geoClm + "))";
                postDB.ExecNonQuery(sqlstr);
            }
            columnName = "x2";
            if (postDB.IsColumnExist(tablename, columnName))
            {
                sqlstr = "update " + tablename + " " + "set " + columnName + "=st_x(endpoint(" + geoClm + "))";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + columnName + " " + "double precision";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + columnName + "=st_x(endpoint(" + geoClm + "))";
                postDB.ExecNonQuery(sqlstr);
            }
            columnName = "y2";
            if (postDB.IsColumnExist(tablename, columnName))
            {
                sqlstr = "update " + tablename + " " + "set " + columnName + "=st_y(endpoint(" + geoClm + "))";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + columnName + " " + "double precision";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + columnName + "=st_y(endpoint(" + geoClm + "))";
                postDB.ExecNonQuery(sqlstr);
            }

            indexName = "level2_index_geom";
            if (postDB.IsIndexExist(tablename, geoClm))
            {
                sqlstr = "drop index " + " " + indexName;
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "create index" + " " + indexName + " " + "on" + " " + tablename + " " + "using gist(" + geoClm + ")";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "create index" + " " + indexName + " " + "on" + " " + tablename + " " + "using gist(" + geoClm + ")";
                postDB.ExecNonQuery(sqlstr);
            }
            #endregion

        }

        /// <summary>
        /// 更新最短路径结果，添加几何字段
        /// </summary>
        /// <param name="ftablename">结果表中数据的来源</param>
        /// <param name="tablename">最短路径结果表</param>
        private void UpdateShortestResult(string ftablename, string tablename)
        {
            string geoColumn, sqlstr, tmpCost;
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
                        sqlstr = "update " + " " + tablename + " " + "set cost=" + curCost.ToString() + " " + "where id2=" + edge_id;
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
                    sqlstr = "update" + " " + tablename + " " + "set cost=" + sumCost.ToString() + " " + "where id2=" + edge_id;
                    postDB.ExecNonQuery(sqlstr);
                }
            }
        }

        /// <summary>
        /// 针对合成的结果表格中总的cost的计算
        /// totalcost需要更新带endpoint对应的记录中
        /// </summary>
        /// <param name="tablename">合成的最短路径结果表</param>
        /// <param name="endpoint">目标结点</param>
        private void ComputeTotalCost(string tablename,string endpoint)
        {
            string sqlstr,tmpCost,tmpId2;
            DataTable dt;
            double oneCost, sumCost;

            oneCost = 0.0;
            sumCost = 0.0;
            sqlstr = "select id2,cost from " + tablename;
            dt=postDB.DoQueryEx(sqlstr);
            for (int i=0;i<dt.Rows.Count;i++)
            {
                tmpCost = dt.Rows[i]["cost"].ToString();
                tmpId2 = dt.Rows[i]["id2"].ToString();
                oneCost = Convert.ToDouble(tmpCost);
                if (! tmpId2.Equals("-1"))
                {
                    sumCost += oneCost;
                }
            }
            
            //假设保存路径结果的表中最后一行为目标点，id2依然为-1，将最后结果更新到这一行中
            sqlstr = "update" + " " + tablename + " " + "set cost=" + sumCost.ToString() + " " + "where id2=-1 and id1=" + endpoint;
            postDB.ExecNonQuery(sqlstr);

        }

    }
}
