#include "pch.h"


namespace
{
	template <typename T>
	bool EnsureCapacity(T*& data, int32_t& capacity, int32_t count)
	{
		if (count <= capacity)
			return true;

		void* ptr = std::realloc(
			data,
			sizeof(T) * static_cast<size_t>(count));

		if (ptr == nullptr)
			return false;

		data = static_cast<T*>(ptr);
		capacity = count;
		return true;
	}


	int32_t CopyContributionToView(
		const VoxelLightContributionV2& source,
		VoxelLightContributionViewV2* view)
	{
		if (view == nullptr)
			return 0;

		const int32_t cellCount = static_cast<int32_t>(source.Cells.size());
		const int32_t sampleCount = static_cast<int32_t>(source.Samples.size());

		if (!EnsureCapacity(view->Cells, view->CellCapacity, cellCount))
		{
			view->CellCount = 0;
			view->SampleCount = 0;
			return 0;
		}

		if (!EnsureCapacity(view->Samples, view->SampleCapacity, sampleCount))
		{
			view->CellCount = 0;
			view->SampleCount = 0;
			return 0;
		}

		if (cellCount > 0)
		{
			std::memcpy(
				view->Cells,
				source.Cells.data(),
				sizeof(VoxelLightContributionCellV2) * static_cast<size_t>(cellCount));
		}

		if (sampleCount > 0)
		{
			std::memcpy(
				view->Samples,
				source.Samples.data(),
				sizeof(VoxelLightContributionSampleV2) * static_cast<size_t>(sampleCount));
		}

		view->CellCount = cellCount;
		view->SampleCount = sampleCount;
		return sampleCount;
	}



	int32_t CopyLightFieldToView(
		const VoxelLightFieldV2& source,
		VoxelLightFieldViewV2* view)
	{
		if (view == nullptr)
			return 0;

		const int32_t lookupCount =
			static_cast<int32_t>(source.Lookup.size());

		const int32_t contributionCount =
			static_cast<int32_t>(source.Contributions.size());

		if (!EnsureCapacity(
			view->Lookup,
			view->LookupCapacity,
			lookupCount))
		{
			view->LookupCount = 0;
			view->ContributionCount = 0;
			return 0;
		}

		if (!EnsureCapacity(
			view->Contributions,
			view->ContributionCapacity,
			contributionCount))
		{
			view->LookupCount = 0;
			view->ContributionCount = 0;
			return 0;
		}

		if (lookupCount > 0)
		{
			std::memcpy(
				view->Lookup,
				source.Lookup.data(),
				sizeof(VoxelLightLookup) *
				static_cast<size_t>(lookupCount));
		}

		if (contributionCount > 0)
		{
			std::memcpy(
				view->Contributions,
				source.Contributions.data(),
				sizeof(VoxelLightGpuContribution) *
				static_cast<size_t>(contributionCount));
		}

		view->Size = source.Size;
		view->LookupCount = lookupCount;
		view->ContributionCount = contributionCount;

		return contributionCount;
	}
}


EXPORT VoxelLightBakerV2* APIENTRY VoxelLightBakerV2Create()
{
	return new VoxelLightBakerV2();
}


EXPORT void APIENTRY VoxelLightBakerV2Destroy(
	VoxelLightBakerV2* baker)
{
	delete baker;
}


EXPORT void APIENTRY VoxelLightBakerV2SetParams(
	VoxelLightBakerV2* baker,
	const VoxelLightBakeParamsV2* params)
{
	if (baker == nullptr || params == nullptr)
		return;

	baker->SetParams(*params);
}


EXPORT void APIENTRY VoxelLightBakerV2SetGrid(
	VoxelLightBakerV2* baker,
	const VoxelGridDesc* grid)
{
	if (baker == nullptr || grid == nullptr)
		return;

	baker->SetGrid(*grid);
}


EXPORT void APIENTRY VoxelLightBakerV2ClearScene(
	VoxelLightBakerV2* baker)
{
	if (baker == nullptr)
		return;

	baker->ClearScene();
}


EXPORT void APIENTRY VoxelLightBakerV2AddMesh(
	VoxelLightBakerV2* baker,
	const Vec3I* origin,
	const Vec3I* size,
	const VoxelData* voxels,
	const VoxelMeshResolvedFace* faces,
	int32_t faceCount)
{
	if (baker == nullptr ||
		origin == nullptr ||
		size == nullptr)
	{
		return;
	}

	baker->AddMesh(
		*origin,
		*size,
		voxels,
		faces,
		faceCount);
}


EXPORT void APIENTRY VoxelLightBakerV2AddGpuMeshFaces(
	VoxelLightBakerV2* baker,
	const GpuVoxelFaceData* faces,
	int32_t faceCount)
{
	if (baker == nullptr)
		return;

	baker->AddGpuMeshFaces(
		faces,
		faceCount);
}


EXPORT VoxelData* APIENTRY VoxelLightBakerV2GetScene(
	VoxelLightBakerV2* baker,
	int32_t* count)
{
	if (baker == nullptr || count == nullptr)
		return nullptr;

	auto* scene = baker->GetScene();

	*count = static_cast<int32_t>(scene->size());

	return scene->data();
}


EXPORT int32_t APIENTRY VoxelLightBakerV2BakePointLight(
	VoxelLightBakerV2* baker,
	const PointLight* light,
	VoxelLightContributionViewV2* contribution)
{
	if (baker == nullptr || light == nullptr)
		return 0;

	VoxelLightContributionV2 result;

	baker->BakePointLight(*light, result);

	return CopyContributionToView(result, contribution);
}


EXPORT int32_t APIENTRY VoxelLightBakerV2BakeAreaLight(
	VoxelLightBakerV2* baker,
	const AreaLight* light,
	VoxelLightContributionViewV2* contribution)
{
	if (baker == nullptr || light == nullptr)
		return 0;

	VoxelLightContributionV2 result;

	baker->BakeAreaLight(*light, result);

	return CopyContributionToView(result, contribution);
}


EXPORT int32_t APIENTRY VoxelLightBakerV2BakeDirectionalLight(
	VoxelLightBakerV2* baker,
	const DirectionalLight* light,
	VoxelLightContributionViewV2* contribution)
{
	if (baker == nullptr || light == nullptr)
		return 0;

	VoxelLightContributionV2 result;

	baker->BakeDirectionalLight(*light, result);

	return CopyContributionToView(result, contribution);
}


EXPORT int32_t APIENTRY VoxelLightBakerV2BakeSpotLight(
	VoxelLightBakerV2* baker,
	const SpotLight* light,
	VoxelLightContributionViewV2* contribution)
{
	if (baker == nullptr || light == nullptr)
		return 0;

	VoxelLightContributionV2 result;

	baker->BakeSpotLight(*light, result);

	return CopyContributionToView(result, contribution);
}


EXPORT void APIENTRY VoxelLightBakerV2ClearLightField(
	VoxelLightBakerV2* baker)
{
	if (baker == nullptr)
		return;

	baker->ClearLightField();
}


EXPORT void APIENTRY VoxelLightBakerV2AccumulateLight(
	VoxelLightBakerV2* baker,
	const VoxelLightContributionViewV2* contribution)
{
	if (baker == nullptr)
		return;

	if (contribution == nullptr)
		return;

	baker->AccumulateLight(
		contribution->Cells,
		contribution->CellCount,
		contribution->Samples,
		contribution->SampleCount);
}


EXPORT int32_t APIENTRY VoxelLightBakerV2GetLightField(
	VoxelLightBakerV2* baker,
	VoxelLightFieldViewV2* field)
{
	if (baker == nullptr || field == nullptr)
		return 0;

	const auto& curField = baker->GetLightField();

	return CopyLightFieldToView(curField, field);
}


EXPORT int32_t APIENTRY VoxelLightBakerV2BuildLightField(
	VoxelLightBakerV2* baker,
	float angularTolerance,
	float relativeEnergyTolerance,
	VoxelLightFieldViewV2* field)
{
	if (baker == nullptr || field == nullptr)
		return 0;

	const auto& result = baker->BuildLightField(angularTolerance, relativeEnergyTolerance);

	return CopyLightFieldToView(result, field);
}


EXPORT VoxelRayMarcherV2* APIENTRY VoxelRayMarcherV2Create(
	VoxelLightBakerV2* baker)
{
	if (baker == nullptr)
		return nullptr;

	auto* marcher = new VoxelRayMarcherV2();

	marcher->SetContext(baker, -1);
	marcher->Prepare(baker->GetVoxelCount());

	return marcher;
}


EXPORT void APIENTRY VoxelRayMarcherV2Destroy(
	VoxelRayMarcherV2* marcher)
{
	delete marcher;
}


EXPORT bool APIENTRY VoxelRayMarcherV2CreateRay(
	VoxelRayMarcherV2* marcher,
	const VoxelLightRay* ray)
{
	if (marcher == nullptr || ray == nullptr)
		return false;

	marcher->ClearContribution();

	return marcher->CreateRay(*ray, 0);
}


EXPORT bool APIENTRY VoxelRayMarcherV2Step(
	VoxelRayMarcherV2* marcher)
{
	if (marcher == nullptr)
		return false;

	if (marcher->StepImpl())
		return true;

	auto& nextRays = marcher->NextRays();

	if (nextRays.empty())
		return false;

	const VoxelLightRay nextRay = nextRays[0];
	const int32_t generation = marcher->Ray().BounceCount + 1;

	nextRays.clear();
	marcher->ClearContribution();

	return marcher->CreateRay(nextRay, generation);
}


EXPORT void APIENTRY VoxelRayMarcherV2GetState(
	VoxelRayMarcherV2* marcher,
	VoxelRayDebugState* state)
{
	if (marcher == nullptr || state == nullptr)
		return;

	marcher->GetDebugState(*state);
}


EXPORT int32_t APIENTRY VoxelRayMarcherV2GetContribution(
	VoxelRayMarcherV2* marcher,
	VoxelLightContributionViewV2* contribution)
{
	if (marcher == nullptr)
		return 0;

	VoxelLightContributionV2 result;
	marcher->GetContribution(result);

	return CopyContributionToView(result, contribution);
}


EXPORT void APIENTRY FreeLightFieldViewV2(
	VoxelLightFieldViewV2* view)
{
	if (view == nullptr)
		return;

	std::free(view->Lookup);
	std::free(view->Contributions);

	view->Size = { 0 };

	view->Lookup = nullptr;
	view->LookupCount = 0;
	view->LookupCapacity = 0;

	view->Contributions = nullptr;
	view->ContributionCount = 0;
	view->ContributionCapacity = 0;
}


EXPORT void APIENTRY FreeContributionViewV2(
	VoxelLightContributionViewV2* view)
{
	if (view == nullptr)
		return;

	std::free(view->Cells);
	std::free(view->Samples);

	view->Cells = nullptr;
	view->CellCount = 0;
	view->CellCapacity = 0;

	view->Samples = nullptr;
	view->SampleCount = 0;
	view->SampleCapacity = 0;
}
