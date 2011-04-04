set DateTime = 2009/09/17,AM
set Datum=European 1950
set UTMZone=31T
set QNH = 1010
set minspeed=0.1

map competitionMap = bitmap(demo.png) grid(1000)


//blank map
//point topleft=SUTM(282000,4632000,0m)
//point bottomright=SUTM(343000,4594000,0m)
//map competitionMap=BLANK(topleft,bottomright) grid(500)

//task 1
TASK Task1 = HWZ()
	POINT Task1_goal1=SUTM (305510,4598570,249m) waypoint(orange)
	POINT Task1_goal2=SUTM (306255,4601283,237m) waypoint(orange)
	POINT Task1_marker=MVMD (1) Marker(orange)
	Point Task1_Target=LNP(Task1_marker,Task1_goal1,Task1_goal2)
	result task1_result=drad(task1_marker,task1_target)
