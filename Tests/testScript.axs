set DateTime = 2010/10/28,PM
set map=..\Europeans2011.png
set Datum=European 1950
set UTMZone=31T
set QNH = 1012

//task 4
TASK Task4 = XDD()
	AREA T4Area = poly(task4area.trk) none()

	filter T4scoringPeriod = BEFORETIME(10:00:00)
	filter T4scoringArea = Inside(T4Area);

	POINT T4A = tafi(T4Area) WAYPOINT(green)
	POINT T4B = tafo(T4Area) waypoint(red)

	//RESULT t4result = D2D(T4A,T4B)
