/* Code Generate 'AddGroupTypeGroupAttribute and AddGroupTypeGroupMemberAttribute' for migrations. 
*/

BEGIN

DECLARE
/* ToDo: Set the @GroupTypeName to the GroupType you are generating Attribute Migrations for*/
@GroupTypeName nvarchar(max) = '#TODO#',  
@crlf varchar(2) = char(13) + char(10)

DECLARE @EntityTypeIdGroup int = (SELECT [Id] FROM [EntityType] WHERE [Name] = 'Rock.Model.Group')
DECLARE @EntityTypeIdGroupMember int = (SELECT [Id] FROM [EntityType] WHERE [Name] = 'Rock.Model.GroupMember')

                
select x.Up from (
SELECT 
        '            RockMigrationHelper.AddGroupTypeGroupAttribute("'+    
		CONVERT(nvarchar(50), GT.[Guid]) + '","' +
        CONVERT(nvarchar(50), FT.[Guid])+ '","' + 
		A.[Name] + '",@"' +
		A.[Description] + '",' +
		CONVERT(varchar(5), A.[Order]) + ',' +
		(CASE 
		 WHEN A.[DefaultValue] IS NULL THEN 'null' 
		 ELSE + '"' + A.[DefaultValue] + '"' 
		 END)+ ',"' +
        CONVERT(nvarchar(50), A.[Guid])+ '");' + @crlf [Up], a.EntityTypeId, gt.[Name], a.[Order]
  FROM [Attribute] A
  INNER JOIN [GroupType] GT ON try_convert(int, A.[EntityTypeQualifierValue]) is not null and GT.Id = convert(int, A.[EntityTypeQualifierValue])
  INNER JOIN [FieldType] FT ON FT.[Id] = A.[FieldTypeId]
  WHERE
	A.[EntityTypeQualifierColumn] = 'GroupTypeId'
	AND A.[EntityTypeId] = @EntityTypeIdGroup
	and gt.[Name] = @GroupTypeName or @GroupTypeName is null
--ORDER BY GT.[Name], A.[Order] 
union all 
SELECT 
        '            RockMigrationHelper.AddGroupTypeGroupMemberAttribute("'+    
		CONVERT(nvarchar(50), GT.[Guid]) + '","' +
        CONVERT(nvarchar(50), FT.[Guid])+ '","' + 
		A.[Name] + '",@"' +
		A.[Description] + '",' +
		CONVERT(varchar(5), A.[Order]) + ',' +
		(CASE 
		 WHEN A.[DefaultValue] IS NULL THEN 'null' 
		 ELSE + '"' + A.[DefaultValue] + '"' 
		 END)+ ',"' +
        CONVERT(nvarchar(50), A.[Guid])+ '");' + @crlf [Up], a.EntityTypeId, gt.[Name], a.[Order]
  FROM [Attribute] A
  INNER JOIN [GroupType] GT ON try_convert(int, A.[EntityTypeQualifierValue]) is not null and GT.Id = convert(int, A.[EntityTypeQualifierValue])
  INNER JOIN [FieldType] FT ON FT.[Id] = A.[FieldTypeId]
  WHERE
	A.[EntityTypeQualifierColumn] = 'GroupTypeId'
	AND A.[EntityTypeId] = @EntityTypeIdGroupMember
	and gt.[Name] = @GroupTypeName or @GroupTypeName is null
) x
ORDER BY x.EntityTypeId, x.[Name], x.[Order] 


SELECT 
        '            RockMigrationHelper.DeleteAttribute("' +    
        CONVERT(nvarchar(50), A.[Guid])+ '");    // GroupType - ' + 
	     ET.FriendlyName + ' Attribute, ' +
		GT.[Name] + ': ' +
		A.[Name] + @crlf [Down]
  FROM [Attribute] A
  INNER JOIN [GroupType] GT ON try_convert(int, A.[EntityTypeQualifierValue]) is not null and GT.Id = convert(int, A.[EntityTypeQualifierValue])
  INNER JOIN [FieldType] FT ON FT.Id = A.[FieldTypeId]
  LEFT OUTER JOIN [EntityType] et on et.Id = a.EntityTypeId
  WHERE A.[EntityTypeQualifierColumn] = 'GroupTypeId' AND A.[EntityTypeId] in (@EntityTypeIdGroup, @EntityTypeIdGroupMember)
  and gt.[Name] = @GroupTypeName or @GroupTypeName is null
ORDER BY a.EntityTypeId, GT.[Name], A.[Order] 

END
