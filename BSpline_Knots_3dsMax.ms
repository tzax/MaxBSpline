-- some of the BSpline code basd on https://github.com/PikminGuts92/BSpliner/
-- runs on a selected spline and generates bspline knots across the shape

global curveDegree = 3
global tarNumKnots = 150

curSpline = selection[1]
print  (classOf curSpline as string) 
if curSpline == undefined or classOf curSpline != line then (
    
    print("Please select a spline")
    
) else (

    if(numKnots curSpline < 4) then (
        
        print("The spline should have more than 4 knots")
    
    ) else (    

        genBspline = GenerateBSpline curSpline curveDegree tarNumKnots;
        
        -- place knots 
        for p in genBspline do
        (
            point pos:p wirecolor:yellow size:5
        )
        
    )
)

-- functions
fn GenerateBSpline controlSpline inDegree inSteps =
(
	local numK = numknots controlSpline;

	if isClosed controlSpline 1 do numK +=1
		
	local invNumK = 1.0 / (numK as double - 1.0);
	local knotsParam = for i = 1 to numK collect invNumK * (i - 1);
	local knotsArray = for i = 1 to numK collect (in coordsys world (interpCurve3D controlSpline 1 knotsParam[i] pathParam:true));

	local pointsPosArr = #(knotsArray[1]);

	local inc = 1.0 / (inSteps - 1.0);
	
	local knotCount = numK + inDegree + 1;
	local knots = GenerateKnotsUniform inDegree knotCount 0.0 1.0;
		
	for i = 1 to inSteps - 1  do
	(		
		local tStep = i as double * inc;
		local outVec = GetBSplinePoint knotsArray inDegree tStep knots;
		append pointsPosArr outVec;
	)

	return  pointsPosArr;
)


fn GenerateKnotsUniform inDegree inCount inMin inMax =
(		
	local order = inDegree + 1;
	local knots = #(inCount); -- Knots
	local frac = (inMax - inMin) / (inCount as double - 2.0 * inDegree - 1.0);  

	for i = 1 to order do
	(
		knots[i] = inMin;
	)
	
	-- Generates knot values - Uniformly
	for  i = order + 1 to inCount - order do
	(
		knots[i] = inMin + (i as double - inDegree as double - 1.0) * frac;
	)
	
	for i = inCount - order + 1 to inCount do
	(
		knots[i] = inMax;
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
		local basis = GetBSplineBasis i inDegree inStep inKnots;
		
		xSum += inPoints[i].x * basis;
		ySum += inPoints[i].y * basis;
		zSum += inPoints[i].z * basis;
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
		if ( inStep >= inKnots[inIndex] and inStep <= inKnots[inIndex + 1]) then 
		(
			return 1.0;		
		) else (
			return 0.0;
		)
	)
	
    local left = 0.0;
    local right = 0.0;
	
	-- Recursive step
	local degM = inDegree - 1;	
	--local outStep = ((inStep - inKnots[inIndex]) / (inKnots[inIndex + inDegree] - inKnots[inIndex])) * GetBSplineBasis inIndex degM inStep inKnots;
	local indP = inIndex + 1;
	--outStep += ((inKnots[inIndex + inDegree + 1] - inStep) / (inKnots[inIndex + inDegree + 1] - inKnots[inIndex + 1])) * GetBSplineBasis indP degM inStep inKnots;
	
	if (inKnots[inIndex + inDegree] != inKnots[inIndex]) then
       left = ((inStep - inKnots[inIndex]) / (inKnots[inIndex + inDegree] - inKnots[inIndex])) * GetBSplineBasis inIndex degM inStep inKnots;

    if (inKnots[inIndex + inDegree + 1] != inKnots[inIndex + 1]) then
        right = ((inKnots[inIndex + inDegree + 1] - inStep) / (inKnots[inIndex + inDegree + 1] - inKnots[inIndex + 1])) * GetBSplineBasis indP degM inStep inKnots;

	return left + right;
)