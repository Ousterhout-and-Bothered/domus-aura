# Identity for GitHub Actions to push images to ECR.
# SSH-based EC2 deploy uses a separate SSH key, not AWS credentials —
# this user has no EC2 or IAM permissions by design.

resource "aws_iam_user" "github_actions" {
  name = "github-actions-deployer"
  path = "/ci/"
}

resource "aws_iam_access_key" "github_actions" {
  user = aws_iam_user.github_actions.name
}

data "aws_caller_identity" "current" {}

resource "aws_iam_policy" "ecr_push" {
  name        = "domus-aura-ecr-push"
  description = "Allows pushing images to the Domus Aura ECR repositories."

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "EcrAuthToken"
        Effect = "Allow"
        Action = [
          "ecr:GetAuthorizationToken"
        ]
        Resource = "*"
      },
      {
        Sid    = "EcrRepositoryPush"
        Effect = "Allow"
        Action = [
          "ecr:BatchCheckLayerAvailability",
          "ecr:BatchGetImage",
          "ecr:CompleteLayerUpload",
          "ecr:GetDownloadUrlForLayer",
          "ecr:InitiateLayerUpload",
          "ecr:PutImage",
          "ecr:UploadLayerPart"
        ]
        Resource = [
          aws_ecr_repository.api.arn,
          aws_ecr_repository.frontend.arn
        ]
      }
    ]
  })
}

resource "aws_iam_user_policy_attachment" "ecr_push" {
  user       = aws_iam_user.github_actions.name
  policy_arn = aws_iam_policy.ecr_push.arn
}