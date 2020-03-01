	
	t1 = timeStamp()
	curSpline = selection[1]
	
	fixedDist = 33.0
	accuracy = 0.0001
	
	-- PathParam of each knot
	numK = numknots curSpline
	if isClosed curSpline 1 do numK +=1
	invNumK = 1.0 / (numK - 1)
	knotsParam = for i = 1 to numK collect invNumK * (i-1)
	knotsArray = for i = 1 to numK collect (in coordsys world (interpCurve3D curSpline 1 knotsParam[i] pathParam:true))
		
	pointsPosArr = #(knotsArray[1])	
	stopLoop = false
	currentKnotIdx = 2
		
	while stopLoop == false do
	(
		-- Find segment where the next point lies
		lastPoint = pointsPosArr[pointsPosArr.count]
		morePoints = false
		for i = currentKnotIdx to numK while not morePoints do
		(
			dist = distance lastPoint  knotsArray[i]
			currentKnotIdx = i

			if dist >= fixedDist do
			(
				morePoints = true
			)
		)
		
		if morePoints then
		(
			-- Let's find the point by bisection in the segment found (max 1000 iterations)
			pt
			error = 1e6
			a = 0.0
			b = 1.0
			iterations = 0
			while error > accuracy and iterations < 1000 do
			(
				iterations += 1
				param = (a+b) * 0.5
				pt = interpBezier3D curSpline 1 (currentKnotIdx - 1) param pathParam:true
				dist = distance lastPoint pt
				if dist > fixedDist then b = param else a = param
				error = abs(dist - fixedDist)
			)
			append pointsPosArr pt
		)
		else
		(
			stopLoop = true
		)
	)
	
	t2 = timeStamp()
	format "Time= %ms\n" (t2-t1)
		
	spl = GenerateBSpline curSpline 2 100;
	--print (spl)
	
	for p in spl do
	(
		point pos:p wirecolor:yellow size:5
	)
	
	for i = 2 to pointsPosArr.count do
	(
		format "dist[%:%]= %\n" (i-1) i (distance pointsPosArr[i] pointsPosArr[i-1])
	)
	
	
	

	
fn GenerateBSpline controlSpline inDegree inSteps =
(
	local numK = numknots controlSpline;

	if isClosed controlSpline 1 do numK +=1
		
	local invNumK = 1.0 / (numK as float - 1.0);
	local knotsParam = for i = 1 to numK collect invNumK * (i - 1);
	local knotsArray = for i = 1 to numK collect (in coordsys world (interpCurve3D controlSpline 1 knotsParam[i] pathParam:true));

	local pointsPosArr = #(knotsArray[1]);

	local inc = 1.0 / inSteps;
	
	-- degree = controlPoints.Count;
	-- # of Knots = m + 1 = p + n + 1
	--   m + 1 = (Control Points - 1) + Degree + 1
	--   m + 1 = Control Points + Degree
	
	local knotCount = numK + inDegree + 1;
	local knots = GenerateKnotsUniform knotCount 0.0 1.0;
	
		--local tStep = 0.0;
		--while tStep <= 1.0 do
		
	for i = 1 to inSteps - 1 do
	(		
		local tStep = i as float * inc;
		local outVec = GetBSplinePoint knotsArray inDegree tStep knots;
		append pointsPosArr outVec;
	)

	return  pointsPosArr;
)


fn GenerateKnotsUniform inCount inMin inMax =
(
	local knots = #(inCount); -- Knots
	local frac = (inMax - inMin) / (inCount as float - 1.0);
	local total = inMin;

	-- Generates knot values - Uniformly
	for  i = 1 to inCount do
	(
		if (i == inCount ) then
		(
			knots[i] = inMax;
			break;
		)

		knots[i] = total;
		total += frac;
	)

	-- Returns knots
	return knots;
)

-- Calculates B-Spline summation of point
-- <param name="points">Control points</param>
-- <param name="p">Degree</param>
-- <param name="t"># of knots - 1</param>
-- <param name="T">Knot vector</param>
fn GetBSplinePoint inPoints inDegree inStep inKnots =
(

	local xSum = 0.0;
	local ySum = 0.0;
	local zSum = 0.0;

	-- Loops for total # of points in list (Summation)
	for i = 1 to inPoints.count do
	(
		-- Calculates B-Spline point for each point (Point * Basis)
		xSum += inPoints[i].x * GetBSplineBasis i inDegree inStep inKnots;
		ySum += inPoints[i].y * GetBSplineBasis i inDegree inStep inKnots;
		zSum += inPoints[i].z * GetBSplineBasis i inDegree inStep inKnots;
	)

	-- Returns B-Spline point fron summation
	return  [xSum, ySum, zSum];
)

-- Recursive basis function for B-Spline
-- <param name="i">Point Index</param>
-- <param name="j">Degree</param>
-- <param name="t">Step (Time)</param>
-- <param name="T">Knots</param>
fn GetBSplineBasis inIndex inDegree inStep inKnots =
(
	-- Base case
	if (inDegree == 0) do
	(
		if (inKnots[inIndex] <= inStep and inStep < inKnots[inIndex + 1]) then 
		(
			return 1.0;		
		) else (
			return 0.0;
		)
	)
	
	-- Recursive step
	local degM = inDegree - 1;	
	local outStep = ((inStep - inKnots[inIndex]) / (inKnots[inIndex + inDegree] - inKnots[inIndex])) * GetBSplineBasis inIndex degM inStep inKnots;
	local indP = inIndex + 1;
	outStep += ((inKnots[inIndex + inDegree + 1] - inStep) / (inKnots[inIndex + inDegree + 1] - inKnots[inIndex + 1])) * GetBSplineBasis indP degM inStep inKnots;

	return outStep;
)