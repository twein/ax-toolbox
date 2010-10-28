set DateTime = 2010/10/28,PM
set map=..\Europeans2011.png
set Datum=European 1950
set UTMZone=31T
set QNH = 1012

//task 4
TASK Task4 = XDD()

AREA T4area = Area(task4area.trk) NONE()
FILTER T4scoringPeriod = BEFORE(10:00:00)

POINT T4A = TAFI(T4area) WAYPOINT(green)
POINT T4B = TALO(T4area) WAYPOINT(red)

RESULT t4result = D2D(T4A,T4B)
