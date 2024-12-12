import axios from "~/plugins/axiosConfig";
import { initializeService } from "~/services/BaseService";
import { AuthenticationAPI } from "~/services/AuthenticationAPI";
import { UserManagementAPI } from "~/services/UserManagementAPI";
import { ClaimManagementAPI } from "~/services/ClaimManagementAPI";
import { RoleManagementAPI } from "~/services/RoleManagementAPI";
import { TutorManagementAPI } from "~/services/TutorManagementAPI";
import { ChildrenManagementAPI } from "~/services/ChildrenManagementAPI";
import { CurriculumManagementAPI } from "~/services/CurriculumManagement";
import { AvailableTimeManagementAPI } from "~/services/AvailableTimeManagementAPI";
import { TutorRequestAPI } from "~/services/TutorRequestAPI";
import { AssessmentManagementAPI } from "~/services/AssessmentManagmentAPI";
import { CertificateAPI } from "~/services/CertificateAPI";
import { WorkExperiencesAPI } from "~/services/WorkExperiencesAPI";
import { StudentProfileAPI } from "~/services/StudentProfileAPI";
import { ProgressReportAPI } from "~/services/ProgressReportAPI";
import { ExerciseManagementAPI } from "~/services/ExerciseManagementAPI";
import { SyllabusManagementAPI } from "~/services/SyllabusManagementAPI";
import { ReviewManagementAPI } from "~/services/ReviewManagementAPI";
import { ScheduleAPI } from "~/services/ScheduleAPI";
import { TimeSlotAPI } from "~/services/TimeSlotAPI";
import { TestManagementAPI } from "~/services/TestManagementAPI";
import { TestQuestionManagementAPI } from "~/services/TestQuestionManagementAPI"
import { TestResultManagementAPI } from "~/services/TestResultManagementAPI"
import { ScoreRangeAPI } from "~/services/ScoreRangeAPI";
import { PackagePaymentAPI } from "~/services/PaymentPackageAPI";
import { NotificationAPI } from "~/services/NotificationAPI";
import { BlogAPI } from "~/services/BlogManagement";
import { PaymentHistoryAPI } from "~/services/PaymentHistoryAPI"
import { ReportManagementAPI } from "~/services/ReportManagementAPI";
import { MessageAPI } from "~/services/MessageAPI";
import { ConversationAPI } from "~/services/ConversationAPI";
import {DashboardManagementAPI} from"~/services/DashboardManagementAPI";

(function () {
  initializeService(axios, "/api");
})();

const services = {
  AuthenticationAPI,
  UserManagementAPI,
  ClaimManagementAPI,
  RoleManagementAPI,
  TutorManagementAPI,
  ChildrenManagementAPI,
  CurriculumManagementAPI,
  AvailableTimeManagementAPI,
  TutorRequestAPI,
  AssessmentManagementAPI,
  CertificateAPI,
  WorkExperiencesAPI,
  StudentProfileAPI,
  ProgressReportAPI,
  ExerciseManagementAPI,
  SyllabusManagementAPI,
  ReviewManagementAPI,
  ScheduleAPI,
  TimeSlotAPI,
  TestManagementAPI,
  TestQuestionManagementAPI,
  TestResultManagementAPI,
  ScoreRangeAPI,
  PackagePaymentAPI,
  BlogAPI,
  NotificationAPI,
  PaymentHistoryAPI,
  ReportManagementAPI,
  MessageAPI,
  ConversationAPI,
  DashboardManagementAPI
};

export default services;
