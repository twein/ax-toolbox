set title=17th FAI European Hot Air Balloon Championship
set subtitle=Lleida, 15-23 september 2011
set DateTime = 2011/09/17,AM
set utcoffset=02:00
set Datum=European 1950
set UTMZone=31T
set tasksinorder=false
set QNH = 1017
set minspeed=0.3
//set altitudecorrectionsfile=..\..\Misc\Logger\insterr.cfg

map competitionMap = bitmap(demo.png) grid(1000)

point seu=sutm(302270,4610028,218m) crosshairs(red)

point rpz2_center=SUTM(295870,4622820,0m) none()
point rpz4_center=SUTM(299790,4599200,0m) none()
point rpz5_center=SUTM(299450,4600160,0m) none()
point rpz6_center=SUTM(301030,4598380,0m) none()
point rpz7_center=SUTM(306370,4603260,0m) none()
point rpz8_center=SUTM(306440,4607560,0m) none()

point launch=TLCH() none()

task task1 = FIN(1)
	filter task1_filterTime=BEFORETIME(10:00:00)
	
	point task1_goal=SUTM(303830,4600360,706ft) none()
	point task1_target=SUTM(303833,4600364,706ft) TARGET(100m,pink)
	point task1_marker=MVMD(1) MARKER(pink)
	result task1_result=DRAD(task1_marker,task1_target,500ft)
	
	restriction task1_dmin=DMIN(launch, task1_goal, 500m, "launch too close to goal 135")


task task2 = HWZ(2)
	filter task2_filterTime=BEFORETIME(10:00:00)
		
	point task2_goal1=SUTM(304890,4600010,750ft) none()
	point task2_goal2=SUTM(304830,4600880,725ft) none()
	point task2_goal3=SUTM(304570,4601630,707ft) none()
	point task2_goal4=SUTM(302330,4598850,769ft) none()	
	
	point task2_target1=SUTM(304887,4600013,750ft) TARGET(100m,yellow)
	point task2_target2=SUTM(304924,4600883,725ft) TARGET(100m,yellow)
	point task2_target3=SUTM(304587,4601634,707ft) TARGET(100m,yellow)
	point task2_target4=SUTM(302420,4598812,769ft) TARGET(100m,yellow)
	
	point task2_marker=MVMD(2) MARKER(yellow)
	point task2_target=LNP(task2_marker,task2_target1,task2_target2,task2_target3,task2_target4,500ft) WAYPOINT(yellow)
	result task2_result=DRAD(task2_marker,task2_target,500ft)
	
	restriction task2_dmin1=DMIN(launch, task2_goal1, 500m, "launch too close to goal 139")
	restriction task2_dmin2=DMIN(launch, task2_goal2, 500m, "launch too close to goal 141")
	restriction task2_dmin3=DMIN(launch, task2_goal3, 500m, "launch too close to goal 143")
	restriction task2_dmin4=DMIN(launch, task2_goal4, 500m, "launch too close to goal 159")



task task3 = PDG(3)
	filter task3_filterTime=BEFORETIME(10:30:00)
	
	point task3_target=MPDGF(1,700ft) TARGET(0m,green)
	point task3_marker=MVMD(3) MARKER(green)
	result task3_result=DRAD10(task3_marker,task3_target,500ft)

	point task3_declarationpoint=TPT(task3_target) target(0m,green)
	restriction task3_dminl=DMIN(task3_target, task3_declarationpoint, 1000m, "declaration too close to goal")
	
	restriction task3_dmin=DMIN(task3_target, task1_goal, 500m, "declared goal too close to goal 135")
	restriction task3_dmin1=DMIN(task3_target, task2_goal1, 500m, "declared goal too close to goal 139")
	restriction task3_dmin2=DMIN(task3_target, task2_goal2, 500m, "declared goal too close to goal 141")
	restriction task3_dmin3=DMIN(task3_target, task2_goal3, 500m, "declared goal too close to goal 143")
	restriction task3_dmin4=DMIN(task3_target, task2_goal4, 500m, "declared goal too close to goal 159")
	
	restriction task3_timesequence=TMIN(task3_target,task3_marker,0,"marker dropped before declaration")

	
	
task task4 = LRN(4)
	filter task4_filter_time=BEFORETIME(10:30:00)

	point task4_marker_a=MVMD(4) MARKER(red)
	point task4_marker_b=MVMD(5) MARKER(red)
	point task4_marker_c=MVMD(6) MARKER(red)
	
	result task4_result=ATRI(task4_marker_a,task4_marker_b,task4_marker_c)
	
	restriction task4_timeinfraction=TMAX(task4_marker_a,task4_marker_c,30,"R15.12: point C out of time limit")
	
	
	
	
task task5 = ANG(5)
	filter task5_filter_time=BEFORETIME(10:30:00)

	point task5_marker_a=MVMD(7) MARKER(blue)
	point task5_marker_b=MVMD(8) MARKER(blue)

	
	result task5_result=ANGSD(task5_marker_a,task5_marker_b,330)
	
	restriction task5_distanceinfraction=DMIN(task5_marker_a,task5_marker_b,500m,"point B too close to A")
	
	





area BPZ1=cylinder(seu,100000m,99999ft,7000ft) none()
//area RPZ2=cylinder(rpz2_center,11000m,3000ft) default(red)
area RPZ4=cylinder(rpz4_center,250m,1200ft) default(red)
area RPZ5=cylinder(rpz5_center,250m,1200ft) default(red)
area RPZ6=cylinder(rpz6_center,250m,1200ft) default(red)
area RPZ7=cylinder(rpz7_center,250m,1200ft) default(red)
area RPZ8=cylinder(rpz8_center,500m,1200ft) default(red)

penalty bpz1=BPZ(BPZ1,"BPZ1")

//penalty rpz2=RPZ(RPZ2,"RPZ2")
penalty rpz4=RPZ(RPZ4,"RPZ4")
penalty rpz5=RPZ(RPZ5,"RPZ5")
penalty rpz6=RPZ(RPZ6,"RPZ6")
penalty rpz7=RPZ(RPZ7,"RPZ7")
penalty rpz8=RPZ(RPZ8,"RPZ8")
