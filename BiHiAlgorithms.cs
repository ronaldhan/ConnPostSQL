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

        //��������ͨ��������ϲ��õ��ģ���ô�������һ��id2=-1�ļ�¼������ͳ��cost
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

            //�����õ��ĵ�·ͼ��
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
                //����ͬһ��
                if (sk.Equals("1"))
                {
                    //λ�ڵ�һ��
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
                    //��Ҫ��������Ƿ����
                    postDB.CheckCreateTable(resultTable, sqlstr);
                    UpdateShortestResult(ftablename, resultTable);
                }
                else
                {
                    //λ�ڵڶ���
                    //��ʾ�������������������
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
                        //λ����ͬ������
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
                        //��Ҫ��������Ƿ����
                        postDB.CheckCreateTable(resultTable, sqlstr);
                        UpdateShortestResult(ftablename, resultTable);
                    }
                    else
                    {
                        //λ�ڲ�ͬ������
                        string sSubTable, eSubTable, entrypointTable, ePoints, eResultTable, midResultTable;
                        DataTable tmpEntryPoint;
                        string transformPoint1, transformPoint2;

                        sSubTable = GetLayerName(ftablename) + "_subtable_" + sSubNum;
                        //�������Ӧ��Ŷ���ε�����ȡ����
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

                        //�ֱ��ҳ�����������ڵ㣬��������������ڵ㵽������ľ���Ӻͣ�ȡ��Сֵ��Ӧ�ĵ�Ϊѡȡ����ڵ�
                        entrypointTable = GetLayerName(ftablename) + "_entrypoint_subgraph";
                        sqlstr = "select entrypoint from " + entrypointTable + " where subnum=" + sSubNum;
                        tmpEntryPoint = postDB.DoQueryEx(sqlstr);
                        ePoints = tmpEntryPoint.Rows[0]["entrypoint"].ToString();
                        transformPoint1 = GetEntryPoint(ePoints, startpoint, endpoint, ftablename);

                        substr = "select * from " + sSubTable;
                        sqlstr = "create table " + resultTable + " as select * from " + algorithmName + "('" + substr + "'," + startpoint + "," + transformPoint1 + ",false,false)";
                        //��Ҫ��������Ƿ����
                        postDB.CheckCreateTable(resultTable, sqlstr);
                        UpdateShortestResult(ftablename, resultTable);

                        sqlstr = "select entrypoint from " + entrypointTable + " where subnum=" + eSubNum;
                        tmpEntryPoint = postDB.DoQueryEx(sqlstr);
                        ePoints = tmpEntryPoint.Rows[0]["entrypoint"].ToString();
                        transformPoint2 = GetEntryPoint(ePoints, startpoint, endpoint, ftablename);

                        substr = "select * from " + eSubTable;
                        eResultTable = GetLayerName(ftablename) + "_path_end";
                        sqlstr = "create table " + eResultTable + " as select * from " + algorithmName + "('" + substr + "'," + transformPoint2 + "," + endpoint + ",false,false)";
                        //������洢����ʱ����
                        postDB.CheckCreateTable(eResultTable, sqlstr);
                        UpdateShortestResult(ftablename, eResultTable);

                        //��������������ڵ�֮�����·����level1�Ͻ���
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
                        //��Ҫ��������Ƿ����
                        postDB.CheckCreateTable(midResultTable, sqlstr);
                        UpdateShortestResult(tablename, midResultTable);

                        //��һ����Ҫ��������ʱ���еĽ�����µ����ս������
                        //�����ȷ��������ʱ��ɾ��
                        sqlstr = "insert into " + " " + resultTable + " " + "select * from " + midResultTable + " " + "where edge_id <> -1";
                        //sqlstr = "insert into " + " " + resultTable + " " + "select * from " + midResultTable;
                        postDB.ExecNonQuery(sqlstr);
                        //�������һ����Ϊ-1�ļ�¼�����ڼ�¼costͳ�ƽ��
                        //sqlstr = "insert into " + " " + resultTable + " " + "select * from " + eSubTable + " " + "where edge_id <> -1";
                        sqlstr = "insert into " + " " + resultTable + " " + "select * from " + eSubTable;
                        postDB.ExecNonQuery(sqlstr);
                        ComputeTotalCost(resultTable,endpoint);
                        //�˴�Ҳ���Կ��ǽ��м��ת����Ҳ���뵽��¼�У���Ϊ�����һ�ֿ��ӻ���ʾ
                        //sqlstr = "insert into " + " " + resultTable + " " + "select * from " + midResultTable;
                        //postDB.ExecNonQuery(sqlstr);
                        //sqlstr = "insert into " + " " + resultTable + " " + "select * from " + eSubTable;
                        //postDB.ExecNonQuery(sqlstr);
                    }

                }
            }
            else
            {
                //���ڲ�ͬ��
                //��Ҫ�ж��ĸ�����ڵײ�
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
                    //��ʼ��λ�ڵײ�����
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

                    //�ֱ���ڵ㣬��������������ڵ㵽������ľ���Ӻͣ�ȡ��Сֵ��Ӧ�ĵ�Ϊѡȡ����ڵ�
                    entrypointTable = GetLayerName(ftablename) + "_entrypoint_subgraph";
                    sqlstr = "select entrypoint from " + entrypointTable + " where subnum=" + sSubNum;
                    tmpEntryPoint = postDB.DoQueryEx(sqlstr);
                    ePoints = tmpEntryPoint.Rows[0]["entrypoint"].ToString();
                    transformPoint = GetEntryPoint(ePoints, startpoint, endpoint, ftablename);

                    //�������·��
                    substr = "select * from " + sSubTable;
                    sqlstr = "create table " + resultTable + " as select * from " + algorithmName + "('" + substr + "'," + startpoint + "," + transformPoint + ",false,false)";
                    //��Ҫ��������Ƿ����
                    postDB.CheckCreateTable(resultTable, sqlstr);
                    UpdateShortestResult(ftablename, resultTable);

                    //��������������ڵ�֮�����·����level1�Ͻ���
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
                    //��Ҫ��������Ƿ����
                    postDB.CheckCreateTable(midResultTable, sqlstr);
                    UpdateShortestResult(tablename, midResultTable);

                    //��һ����Ҫ��������ʱ���еĽ�����µ����ս������
                    //�����ȷ��������ʱ��ɾ��
                    //sqlstr = "insert into " + " " + resultTable + " " + "select * from " + midResultTable + " " + "where edge_id <> -1";
                    sqlstr = "insert into " + " " + resultTable + " " + "select * from " + midResultTable;
                    postDB.ExecNonQuery(sqlstr);
                    ComputeTotalCost(resultTable,endpoint);

                }
                else
                {
                    //��ֹ��λ�ڵײ�����
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

                    //�ֱ���ڵ㣬��������������ڵ㵽������ľ���Ӻͣ�ȡ��Сֵ��Ӧ�ĵ�Ϊѡȡ����ڵ�
                    entrypointTable = GetLayerName(ftablename) + "_entrypoint_subgraph";
                    sqlstr = "select entrypoint from " + entrypointTable + " where subnum=" + eSubNum;
                    tmpEntryPoint = postDB.DoQueryEx(sqlstr);
                    ePoints = tmpEntryPoint.Rows[0]["entrypoint"].ToString();
                    transformPoint = GetEntryPoint(ePoints, startpoint, endpoint, ftablename);

                    //��������������ڵ�֮�����·����level1�Ͻ���
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
                    //��Ҫ��������Ƿ����
                    postDB.CheckCreateTable(resultTable, sqlstr);
                    UpdateShortestResult(tablename, resultTable);

                    //�������·��
                    eResultTable = GetLayerName(ftablename) + "_path_end";
                    substr = "select * from " + eSubTable;
                    sqlstr = "create table " + eResultTable + " as select * from " + algorithmName + "('" + substr + "'," + transformPoint + "," + endpoint + ",false,false)";
                    //��Ҫ��������Ƿ����
                    postDB.CheckCreateTable(eResultTable, sqlstr);
                    UpdateShortestResult(ftablename, eResultTable);

                    //��һ����Ҫ��������ʱ���еĽ�����µ����ս������
                    //�����ȷ��������ʱ��ɾ��
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
        /// ��������ʵ���ڵ�
        /// </summary>
        /// <param name="ePoints">��ѡ����ڵ��ַ���</param>
        /// <param name="startpoint">��ʼ��</param>
        /// <param name="endpoint">��ֹ��</param>
        /// <param name="ftablename">ͼ������</param>
        /// <returns></returns>
        private string GetEntryPoint(string ePoints, string startpoint, string endpoint, string ftablename)
        {
            //��һ���������ڵ���ѡ����ʵ�һ�����������С
            string[] points;
            string tablename, sqlstr, tmpPoint;
            double xs, ys, xt, yt, xe, ye;
            double distST, distTE;
            Hashtable ht;
            DataTable tmpdt;
            string[] keyPoints;
            double[] valuePoints;

            tablename = GetLayerName(ftablename) + "_vertex_adjaction";
            //ʹ�ñ�׼����st_x��st_y
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
        /// Ϊʹ�õ�ͼ�����length�ֶ�
        /// </summary>
        private void SetRoadLength()
        {
            //���õ�·��length
            string tablename, sqlstr, colName, geoClm, projectsrid, columnName;
            //Ϊ��·ͼ���geometry���index
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
        /// �������·���������Ӽ����ֶ�
        /// </summary>
        /// <param name="ftablename">����������ݵ���Դ</param>
        /// <param name="tablename">���·�������</param>
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
            //���������ʼ�������ֹ�㣬��Ӧ��edge_idΪ-1����Ҫ����
            sqlstr = "select * from " + tablename + " order by seq asc";
            tmpdt = postDB.DoQueryEx(sqlstr);
            for (int i = 0; i <= tmpdt.Rows.Count - 1; i++)
            {
                //�ڶ��ж�Ӧ���Ǳߵ�id
                string edge_id = tmpdt.Rows[i]["id2"].ToString();
                tmpCost = tmpdt.Rows[i]["cost"].ToString();
                isZero = tmpCost.Equals("0") ? true : false;
                string tmpGeom;
                if (!edge_id.Equals("-1"))
                {
                    //˫���㷨����ЩcostΪ0����ʱ��Ҫ��ѯ������ֵ
                    sqlstr = "select st_astext(geom) as geomtext,length from " + " " + ftablename + " " + "where gid=" + edge_id;
                    tmpresult = postDB.DoQueryEx(sqlstr);
                    tmpGeom = tmpresult.Rows[0]["geomtext"].ToString();
                    //����cost�ܺ�
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
                    //����ط��漰��һ�����⣬����Ѿ���newlines�еļ����ֶν���ͶӰת�䣿�˴�����ʽ��Ҫ�ı�
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
        /// ��ԺϳɵĽ��������ܵ�cost�ļ���
        /// totalcost��Ҫ���´�endpoint��Ӧ�ļ�¼��
        /// </summary>
        /// <param name="tablename">�ϳɵ����·�������</param>
        /// <param name="endpoint">Ŀ����</param>
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
            
            //���豣��·������ı������һ��ΪĿ��㣬id2��ȻΪ-1������������µ���һ����
            sqlstr = "update" + " " + tablename + " " + "set cost=" + sumCost.ToString() + " " + "where id2=-1 and id1=" + endpoint;
            postDB.ExecNonQuery(sqlstr);

        }

    }
}
