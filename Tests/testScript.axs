set DateTime = 2010/10/28,PM
//set map=..\Europeans2011.png
set map=blank
set Datum=European 1950
set UTMZone=31T
set QNH = 1012
set TASKORDER=free

//task 4
TASK Task4 = XDD()
	POINT T4AreaCenter = SUTM (31T,300804,4603554,180m) none()
	AREA T4Area = circle(T4AreaCenter,200m) Default(green)
	//AREA T4AnotherArea = poly(task4area.trk) Default(green)

	filter T4scoringPeriod = BEFORETIME(10:00:00)
	filter T4scoringArea = Inside(T4Area);

	POINT T4A = tafi(T4Area) marker(green)
	POINT T4B = tafo(T4Area) marker(red)

	//RESULT t4result = D2D(T4A,T4B)
