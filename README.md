Man Hour Estimation:

Requirement Assessment - 0.5 Hours
Execution Planning - 0.5 Hours
Execution - 1.5 Hours
Testing - 0.5 Hours
Bug Fixing - 0.5 Hours

Total - 3.5 Hours

Part 2 ANSWER:

   Assessment PART 2
        SELECT
        p.UniqueName AS PlatformName,
        w.Id,
        w.PlatformId,
        w.UniqueName,
        w.Latitude,
        w.Longitude,
        w.CreatedAt,
        w.UpdatedAt
    FROM Wells w
    INNER JOIN Platforms p ON w.PlatformId = p.Id
    INNER JOIN (
        SELECT PlatformId, MAX(UpdatedAt) AS LastUpdatedAt
        FROM Wells
        GROUP BY PlatformId
    ) latest ON w.PlatformId = latest.PlatformId
            AND w.UpdatedAt = latest.LastUpdatedAt
    ORDER BY p.Id
