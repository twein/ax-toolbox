set DateTime = 2010/10/28,PM
set Datum=European 1950
set UTMZone=31T
set QNH = 1012

map competitionMap = bitmap(demo.png) grid(1000)


//blank map
//point topleft=SUTM(282000,4632000,0m)
//point bottomright=SUTM(343000,4594000,0m)
//map competitionMap=BLANK(topleft,bottomright) grid(500)

//task 4
TASK Task4 = XDD()
	POINT PWaypoint=SUTM (300000,4603000,180m) waypoint(orange)
	POINT PMarker=SUTM (300500,4603000,180m) Marker(green)
	POINT PCrosshairs=SUTM (300000,4603500,180m) crosshairs(red)
	POINT PTarget=SUTM (300500,4603500,180m) target(100m, yellow)


	POINT T4AreaCenter = SUTM (302000,4605000,180m) none()
	AREA T4Area = circle(T4AreaCenter,1000m) Default(green)
	//AREA T4AnotherArea = poly(task4area.trk) Default(green)

	filter T4scoringPeriod = BEFORETIME(19:00:00)
	filter T4scoringArea = Inside(T4Area);

	POINT T4A = tafi(T4Area) marker(green)
	POINT T4B = tafo(T4Area) marker(red)

	//RESULT t4result = D2D(T4A,T4B)

TASK Task5 = PDG()
TASK Task6 = LRN()
TASK Task7 = ANG()
TASK Task8 = JDG()
