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
	POINT Task1_goal1=SUTM(305510,4598570,249m) waypoint(orange)
	POINT Task1_goal2=SUTM(306255,4601283,237m) waypoint(orange)
	POINT Task1_marker=MVMD(1) Marker(orange)
	Point Task1_Target=LNP(Task1_marker,Task1_goal1,Task1_goal2)
	result task1_result=drad(task1_marker,task1_target)

//task 2
task Task2 = FIN()
	filter Task2_filter=afterpoint(Task1_marker)
	point Task2_goal=sutm(304360,4602600,215m) target(100m,blue)
	POINT Task2_marker=MVMD(2) Marker(blue)
	result task2_result=drad(task2_marker,task2_goal)

//task 3
task Task3 = 3DT()
	area task3_area=POLY(demoarea.trk) default(green)
	filter task3_filter=inside(task3_area)
	point task3_point1=tafi(task3_area) Marker(green)
	point task3_point2=talo(task3_area) Marker(green)
	result task3_result=dacc(task3_point1,task3_point2)

//task 4
task Task4 = RTA()
	filter Task4_filter=afterpoint(Task2_marker)
	POINT Task4_marker1=MVMD(3) Marker(pink)
	point Task4_marker2=tdd(Task4_marker1,3000m)
	result task4_result=tsec(Task4_marker1,Task4_marker2)

	