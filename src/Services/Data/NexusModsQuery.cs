namespace ModManager.Services.Data;

public static class NexusModsQuery
{
	public static readonly string CollectionRevision = @"
query collectionRevision($slug: String, $adult: Boolean, $domain: String, $revision: Int) {
    collectionRevision(slug: $slug, viewAdultContent: $adult, domainName: $domain, revision: $revision) {
		externalResources {
			author
			collectionRevisionId
			fileExpression
			id
			name
			optional
			resourceType
			resourceUrl
			version
		}
		collection {
			id
			slug
			name
			summary
			category {
				name
			}
			adultContent
			overallRating
			overallRatingCount
			endorsements
			totalDownloads
			draftRevisionNumber
			latestPublishedRevision {
				fileSize
				modCount
			}
			user {
				memberId
				avatar
				name
			}
			tileImage {
				url
				altText
				thumbnailUrl(size: small)
			}
      	}
		revisionNumber
		collectionChangelog {
			createdAt
			description
			id
		}
		assetsSizeBytes
		createdAt
		updatedAt
		downloadLink
		fileSize
		gameVersions {
			id
			reference
		}
		rating {
			average
			total
		}
		status
		modFiles {
			collectionRevisionId
			optional
			version
			file {
				modId
				fileId
				size
    			sizeInBytes
				name
				version
				description
				uri
				primary
				owner {
					name
					avatar
					memberId
				}
				mod {
					author
					category
					modCategory {
						id
						name
					}
					name
					pictureUrl
					status
					summary
					uploader {
						name
						avatar
						memberId
					}
					version
				}
			}
		}
    }
}
";
}
