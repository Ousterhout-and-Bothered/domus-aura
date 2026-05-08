# IAM role for the EC2 host to pull images from ECR.
# Attached to the instance via an instance profile.
# Replaces long-lived credentials on the host with auto-rotated role credentials.

resource "aws_iam_role" "ec2_ecr_pull" {
  name = "domus-aura-ec2-ecr-pull"
  path = "/ec2/"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          Service = "ec2.amazonaws.com"
        }
        Action = "sts:AssumeRole"
      }
    ]
  })
}

resource "aws_iam_policy" "ec2_ecr_pull" {
  name        = "domus-aura-ec2-ecr-pull"
  description = "Allows the EC2 host to pull images from Domus Aura ECR repositories."

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid      = "EcrAuthToken"
        Effect   = "Allow"
        Action   = ["ecr:GetAuthorizationToken"]
        Resource = "*"
      },
      {
        Sid    = "EcrRepositoryPull"
        Effect = "Allow"
        Action = [
          "ecr:BatchCheckLayerAvailability",
          "ecr:BatchGetImage",
          "ecr:GetDownloadUrlForLayer"
        ]
        Resource = [
          aws_ecr_repository.api.arn,
          aws_ecr_repository.frontend.arn
        ]
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "ec2_ecr_pull" {
  role       = aws_iam_role.ec2_ecr_pull.name
  policy_arn = aws_iam_policy.ec2_ecr_pull.arn
}

resource "aws_iam_instance_profile" "ec2_ecr_pull" {
  name = "domus-aura-ec2-ecr-pull"
  role = aws_iam_role.ec2_ecr_pull.name
}