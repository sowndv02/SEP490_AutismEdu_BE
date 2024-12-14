import { Box, Breadcrumbs, Paper, Stack, Typography } from '@mui/material';
import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import LoadingComponent from '~/components/LoadingComponent';
import services from '~/plugins/services';
import PAGES from '~/utils/pages';
import AcceptDialog from '../handleDialog/AcceptDialog';
import BasicInformation from './BasicInformation';
import TutorCertificate from './TutorCertificate';
import TutorCurriculum from './TutorCurriculum';
import TutorWorkExperience from './TutorWorkExperience';

function TutorRegistrationDetail() {
    const { id } = useParams();
    const [tutorInformation, setTutorInformation] = useState();
    const [certificates, setCertificates] = useState([]);
    const [curriculums, setCurriculums] = useState([]);
    const [workExperiences, setWorkExperiences] = useState([]);
    const [loading, setLoading] = useState(false);
    useEffect(() => {
        const handleGetTutorRegistration = async () => {
            try {
                setLoading(true);
                await services.TutorManagementAPI.handleGetTutorRegisterDetail(id, (res) => {
                    const { certificates, curriculums, workExperiences, ...tutorInfo } = res.result
                    setTutorInformation(tutorInfo);
                    setCertificates(certificates);
                    setCurriculums(curriculums);
                    setWorkExperiences(workExperiences);
                }, (err) => {
                    console.log(err);
                })
            } catch (error) {
                console.log(error);
            } finally {
                setLoading(false)
            }
        }
        handleGetTutorRegistration();
    }, [])
    return (
        <Box>
            <Breadcrumbs aria-label="breadcrumb">
                <Link
                    underline="hover"
                    color="inherit"
                    to={PAGES.TUTORREGISTRATIONMANAGEMENT}
                >
                    Danh sách dơn đăng ký
                </Link>
                <Typography sx={{ color: 'text.primary' }}>Chi tiết đơn đăng ký</Typography>
            </Breadcrumbs>
            <Paper variant='elevation' sx={{ p: 2, mt: 2 }}>
                <Stack direction='row' alignItems="center" justifyContent="space-between">
                    <Box sx={{ maxWidth: "50%" }}>
                        <Typography><span>Trạng thái đơn:</span>{
                            tutorInformation?.requestStatus === 0 && <span style={{ color: "red", marginLeft: "20px" }}>Đã từ chối</span>
                        }
                            {
                                tutorInformation?.requestStatus === 1 && <span style={{ color: "green", marginLeft: "20px" }}>Đã chấp nhận</span>
                            }
                            {
                                tutorInformation?.requestStatus === 2 && <span style={{ color: "blue", marginLeft: "20px" }}>Đang chờ</span>
                            }</Typography>
                        {
                            tutorInformation?.requestStatus === 0 && (
                                <Typography>Lý do từ chối: <span style={{ color: "red", marginLeft: "20px" }}>{tutorInformation?.rejectionReason}</span></Typography>
                            )
                        }
                    </Box>
                    {
                        tutorInformation?.requestStatus === 2 && (
                            <Box>
                                <AcceptDialog status={1} id={tutorInformation.id}
                                    setTutorInformation={setTutorInformation}
                                    setCurriculums={setCurriculums}
                                    setCertificates={setCertificates}
                                    setWorkExperiences={setWorkExperiences} />
                                <AcceptDialog status={0} id={tutorInformation.id}
                                    setTutorInformation={setTutorInformation}
                                    setCurriculums={setCurriculums}
                                    setCertificates={setCertificates}
                                    setWorkExperiences={setWorkExperiences} />
                            </Box>
                        )
                    }

                    {
                        tutorInformation?.requestStatus !== 2 && tutorInformation?.approvedBy && (
                            <Typography>Người xử lý: {tutorInformation?.approvedBy?.email}</Typography>
                        )
                    }
                </Stack>
            </Paper>
            <BasicInformation information={tutorInformation} certificates={certificates} setCertificates={setCertificates} />
            {
                tutorInformation && (
                    <>
                        <Stack direction='row' gap={2} mt={3}>
                            <Box sx={{ width: "50%" }}>
                                <TutorCertificate id={tutorInformation.id} certificates={certificates} setCertificates={setCertificates} />
                                <TutorWorkExperience id={tutorInformation.id} workExperiences={workExperiences} setWorkExperiences={setWorkExperiences} />
                            </Box>
                            <Box sx={{ width: "50%" }}>
                                <TutorCurriculum id={tutorInformation.id} curriculums={curriculums} setCurriculums={setCurriculums} />
                            </Box>
                        </Stack>
                    </>
                )
            }
            <LoadingComponent open={loading} />
        </Box>
    )
}

export default TutorRegistrationDetail
