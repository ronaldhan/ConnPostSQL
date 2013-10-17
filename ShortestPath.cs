using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace ConnPostSQL
{
    /// <summary>
    /// ���ʹ��PostgreSQL���ṩ�ĺ������Dijkstra�㷨��Astar�㷨��Shooting-Star�㷨
    /// ���Զ������������н�һ���ĳ���ʹ��case���ӽ����жϣ�����������
    /// PostgreSQL�б���ΪСд���ʴ����б�����ΪСд
    /// </summary>
    class ShortestPath
    {
        private DB postDB;
        public ShortestPath(DB outDB)
        {
            postDB = outDB;
        }

        #region pg 1.0�Ĵ���
        /*
        public void Dijkstra(string ftablename, string startpoint, string endpoint)
        {
            //���м���ǰ��׼����������Ϊlength�ֶθ�ֵ���������������
            //�ڴ˴���ɹ����������������������м������Ʊ���������еĵ�·���輸��ʵ��
            string sqlstr, substr;
            string tablename;
            tablename = GetLayerName(ftablename) + "_dijkstra";
            substr = "select gid as id,source::integer,target::integer,length::double precision as cost from " + ftablename;
            sqlstr = "create table " + tablename + " as select * from shortest_path('" + substr + "'," + startpoint + "," + endpoint + ",false,false)";
            //��Ҫ��������Ƿ����
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
        
#region 2.0 ���´���
        //shootingstar�����岻�Ǻܴ󣬲�����2.0��û���ṩ�˺���������Ϊֹû����1003����������ȥ
        //�������㷨�ĺ������кϲ�����Ϊ���ݽ����Ĳ��������㷨����֮����������ͬ
        //���³���Ϊһ�����������㷨���ƽ���switch case�ж�
        //�µ��㷨�������ֻ���г�����Ӧ�Ķ���ͱߵı�ţ��Լ���Ӧ�ĺķ�ֵ����û�м���ʵ�壬����Ҫ���
        //���а�װ������������ʾ�ڵ�ͼ�У��˲��ֹ��������������ã�����ֱ�ӷ�װ��������
        //20131004���£���pgr�ṩ��˫���㷨��װ��������
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

        //���أ���Ҫ����������������
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

        //Ϊ���·���Ľ����Ӽ���ʵ�壬�ܹ��ڵ�ͼ����ʾ
        //20131004 Ϊ���еļ�����ͳ����cost��д�뵽id2=-1�ļ�¼��cost�ֶ���
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
                        sqlstr = "update " + " " + tablename + " " + "set cost=" + curCost.ToString() + " " +"where id2=" + edge_id;
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
                    sqlstr = "update" + " " + tablename + " " + "set cost=" + sumCost.ToString() + " "+ "where id2=" + edge_id;
                    postDB.ExecNonQuery(sqlstr);
                }
            }
        }
  

    }
}
