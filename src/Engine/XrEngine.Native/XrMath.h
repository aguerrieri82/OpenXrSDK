#pragma once

struct Vec3I
{
	int32_t X;
	int32_t Y;
	int32_t Z;

	constexpr Vec3I operator+(const Vec3I& other) const
	{
		return { X + other.X, Y + other.Y, Z + other.Z };
	}

	constexpr Vec3I operator-(const Vec3I& other) const
	{
		return { X - other.X, Y - other.Y, Z - other.Z };
	}

	constexpr Vec3I operator*(int32_t scalar) const
	{
		return { X * scalar, Y * scalar, Z * scalar };
	}

	constexpr Vec3I operator/(int32_t scalar) const
	{
		return { X / scalar, Y / scalar, Z / scalar };
	}

	constexpr Vec3I& operator+=(const Vec3I& other)
	{
		X += other.X;
		Y += other.Y;
		Z += other.Z;
		return *this;
	}

	constexpr Vec3I& operator-=(const Vec3I& other)
	{
		X -= other.X;
		Y -= other.Y;
		Z -= other.Z;
		return *this;
	}

	constexpr Vec3I& operator*=(int32_t scalar)
	{
		X *= scalar;
		Y *= scalar;
		Z *= scalar;
		return *this;
	}

	constexpr Vec3I& operator/=(int32_t scalar)
	{
		X /= scalar;
		Y /= scalar;
		Z /= scalar;
		return *this;
	}
};

constexpr Vec3I operator*(int32_t scalar, const Vec3I& value)
{
	return value * scalar;
}

struct Vec2
{
	float X;
	float Y;

	constexpr Vec2 operator+() const
	{
		return *this;
	}

	constexpr Vec2 operator-() const
	{
		return { -X, -Y };
	}

	constexpr Vec2 operator+(const Vec2& other) const
	{
		return { X + other.X, Y + other.Y };
	}

	constexpr Vec2 operator-(const Vec2& other) const
	{
		return { X - other.X, Y - other.Y };
	}

	constexpr Vec2 operator*(const Vec2& other) const
	{
		return { X * other.X, Y * other.Y };
	}

	constexpr Vec2 operator/(const Vec2& other) const
	{
		return { X / other.X, Y / other.Y };
	}

	constexpr Vec2 operator*(float scalar) const
	{
		return { X * scalar, Y * scalar };
	}

	constexpr Vec2 operator/(float scalar) const
	{
		return { X / scalar, Y / scalar };
	}

	constexpr Vec2& operator+=(const Vec2& other)
	{
		X += other.X;
		Y += other.Y;
		return *this;
	}

	constexpr Vec2& operator-=(const Vec2& other)
	{
		X -= other.X;
		Y -= other.Y;
		return *this;
	}

	constexpr Vec2& operator*=(const Vec2& other)
	{
		X *= other.X;
		Y *= other.Y;
		return *this;
	}

	constexpr Vec2& operator/=(const Vec2& other)
	{
		X /= other.X;
		Y /= other.Y;
		return *this;
	}

	constexpr Vec2& operator*=(float scalar)
	{
		X *= scalar;
		Y *= scalar;
		return *this;
	}

	constexpr Vec2& operator/=(float scalar)
	{
		X /= scalar;
		Y /= scalar;
		return *this;
	}

	constexpr float LengthSquared() const
	{
		return X * X + Y * Y;
	}

	float Length() const
	{
		return std::sqrt(LengthSquared());
	}

	Vec2 Normalized() const
	{
		const float length = Length();
		return length > 0.0f ? *this / length : Vec2{};
	}
};

constexpr Vec2 operator*(float scalar, const Vec2& value)
{
	return value * scalar;
}

constexpr float Dot(const Vec2& a, const Vec2& b)
{
	return a.X * b.X + a.Y * b.Y;
}

constexpr Vec2 Min(const Vec2& a, const Vec2& b)
{
	return {
		std::min(a.X, b.X),
		std::min(a.Y, b.Y)
	};
}

constexpr Vec2 Max(const Vec2& a, const Vec2& b)
{
	return {
		std::max(a.X, b.X),
		std::max(a.Y, b.Y)
	};
}

constexpr Vec2 Lerp(const Vec2& a, const Vec2& b, float t)
{
	return a + (b - a) * t;
}

struct Vec3
{
	float X;
	float Y;
	float Z;

	constexpr Vec3 operator+() const
	{
		return *this;
	}

	constexpr Vec3 operator-() const
	{
		return { -X, -Y, -Z };
	}

	constexpr Vec3 operator+(const Vec3& other) const
	{
		return { X + other.X, Y + other.Y, Z + other.Z };
	}

	constexpr Vec3 operator-(const Vec3& other) const
	{
		return { X - other.X, Y - other.Y, Z - other.Z };
	}

	constexpr Vec3 operator*(const Vec3& other) const
	{
		return { X * other.X, Y * other.Y, Z * other.Z };
	}

	constexpr Vec3 operator/(const Vec3& other) const
	{
		return { X / other.X, Y / other.Y, Z / other.Z };
	}

	constexpr Vec3 operator*(float scalar) const
	{
		return { X * scalar, Y * scalar, Z * scalar };
	}

	constexpr Vec3 operator/(float scalar) const
	{
		return { X / scalar, Y / scalar, Z / scalar };
	}

	constexpr Vec3& operator+=(const Vec3& other)
	{
		X += other.X;
		Y += other.Y;
		Z += other.Z;
		return *this;
	}

	constexpr Vec3& operator-=(const Vec3& other)
	{
		X -= other.X;
		Y -= other.Y;
		Z -= other.Z;
		return *this;
	}

	constexpr Vec3& operator*=(const Vec3& other)
	{
		X *= other.X;
		Y *= other.Y;
		Z *= other.Z;
		return *this;
	}

	constexpr Vec3& operator/=(const Vec3& other)
	{
		X /= other.X;
		Y /= other.Y;
		Z /= other.Z;
		return *this;
	}

	constexpr Vec3& operator*=(float scalar)
	{
		X *= scalar;
		Y *= scalar;
		Z *= scalar;
		return *this;
	}

	constexpr Vec3& operator/=(float scalar)
	{
		X /= scalar;
		Y /= scalar;
		Z /= scalar;
		return *this;
	}

	constexpr float LengthSquared() const
	{
		return X * X + Y * Y + Z * Z;
	}

	float Length() const
	{
		return std::sqrt(LengthSquared());
	}

	Vec3 Normalized() const
	{
		const float length = Length();
		return length > 0.0f ? *this / length : Vec3{};
	}
};

constexpr Vec3 operator*(float scalar, const Vec3& value)
{
	return value * scalar;
}

constexpr float Dot(const Vec3& a, const Vec3& b)
{
	return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
}

constexpr Vec3 Cross(const Vec3& a, const Vec3& b)
{
	return {
		a.Y * b.Z - a.Z * b.Y,
		a.Z * b.X - a.X * b.Z,
		a.X * b.Y - a.Y * b.X
	};
}

constexpr Vec3 Min(const Vec3& a, const Vec3& b)
{
	return {
		std::min(a.X, b.X),
		std::min(a.Y, b.Y),
		std::min(a.Z, b.Z)
	};
}

constexpr Vec3 Max(const Vec3& a, const Vec3& b)
{
	return {
		std::max(a.X, b.X),
		std::max(a.Y, b.Y),
		std::max(a.Z, b.Z)
	};
}

constexpr Vec3 Lerp(const Vec3& a, const Vec3& b, float t)
{
	return a + (b - a) * t;
}

constexpr Vec3 Reflect(const Vec3& direction, const Vec3& normal)
{
	return direction - normal * (2.0f * Dot(direction, normal));
}

struct Vec4
{
	float X;
	float Y;
	float Z;
	float W;

	constexpr Vec4 operator+() const
	{
		return *this;
	}

	constexpr Vec4 operator-() const
	{
		return { -X, -Y, -Z, -W };
	}

	constexpr Vec4 operator+(const Vec4& other) const
	{
		return {
			X + other.X,
			Y + other.Y,
			Z + other.Z,
			W + other.W
		};
	}

	constexpr Vec4 operator-(const Vec4& other) const
	{
		return {
			X - other.X,
			Y - other.Y,
			Z - other.Z,
			W - other.W
		};
	}

	constexpr Vec4 operator*(const Vec4& other) const
	{
		return {
			X * other.X,
			Y * other.Y,
			Z * other.Z,
			W * other.W
		};
	}

	constexpr Vec4 operator/(const Vec4& other) const
	{
		return {
			X / other.X,
			Y / other.Y,
			Z / other.Z,
			W / other.W
		};
	}

	constexpr Vec4 operator*(float scalar) const
	{
		return { X * scalar, Y * scalar, Z * scalar, W * scalar };
	}

	constexpr Vec4 operator/(float scalar) const
	{
		return { X / scalar, Y / scalar, Z / scalar, W / scalar };
	}

	constexpr Vec4& operator+=(const Vec4& other)
	{
		X += other.X;
		Y += other.Y;
		Z += other.Z;
		W += other.W;
		return *this;
	}

	constexpr Vec4& operator-=(const Vec4& other)
	{
		X -= other.X;
		Y -= other.Y;
		Z -= other.Z;
		W -= other.W;
		return *this;
	}

	constexpr Vec4& operator*=(const Vec4& other)
	{
		X *= other.X;
		Y *= other.Y;
		Z *= other.Z;
		W *= other.W;
		return *this;
	}

	constexpr Vec4& operator/=(const Vec4& other)
	{
		X /= other.X;
		Y /= other.Y;
		Z /= other.Z;
		W /= other.W;
		return *this;
	}

	constexpr Vec4& operator*=(float scalar)
	{
		X *= scalar;
		Y *= scalar;
		Z *= scalar;
		W *= scalar;
		return *this;
	}

	constexpr Vec4& operator/=(float scalar)
	{
		X /= scalar;
		Y /= scalar;
		Z /= scalar;
		W /= scalar;
		return *this;
	}

	constexpr float LengthSquared() const
	{
		return X * X + Y * Y + Z * Z + W * W;
	}

	float Length() const
	{
		return std::sqrt(LengthSquared());
	}

	Vec4 Normalized() const
	{
		const float length = Length();
		return length > 0.0f ? *this / length : Vec4{};
	}
};

constexpr Vec4 operator*(float scalar, const Vec4& value)
{
	return value * scalar;
}

constexpr float Dot(const Vec4& a, const Vec4& b)
{
	return a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
}

constexpr Vec4 Min(const Vec4& a, const Vec4& b)
{
	return {
		std::min(a.X, b.X),
		std::min(a.Y, b.Y),
		std::min(a.Z, b.Z),
		std::min(a.W, b.W)
	};
}

constexpr Vec4 Max(const Vec4& a, const Vec4& b)
{
	return {
		std::max(a.X, b.X),
		std::max(a.Y, b.Y),
		std::max(a.Z, b.Z),
		std::max(a.W, b.W)
	};
}

constexpr Vec4 Lerp(const Vec4& a, const Vec4& b, float t)
{
	return a + (b - a) * t;
}



struct Bounds3
{
	Vec3 Max;
	Vec3 Min;
};
