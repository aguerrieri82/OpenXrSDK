#pragma once

#include <algorithm>
#include <cmath>
#include <cstdio>
#include <list>
#include <memory>
#include <stdexcept>
#include <vector>


#include "IK_QJacobian.h"
#include "IK_QJacobianSolver.h"
#include "IK_QSegment.h"
#include "IK_QTask.h"
#include "IK_solver.h"

#include <Eigen/Jacobi>
#include <Eigen/Householder>
#include <Eigen/src/QR/ColPivHouseholderQR.h>
#include <Eigen/src/misc/RealSvd2x2.h>
#include <Eigen/src/SVD/SVDBase.h>
#include <Eigen/src/SVD/JacobiSVD.h>

