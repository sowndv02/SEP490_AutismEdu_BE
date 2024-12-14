import { Box, Breadcrumbs, Divider, Paper, Stack, Typography } from '@mui/material';
import Button from '@mui/material/Button';
import Step from '@mui/material/Step';
import StepLabel from '@mui/material/StepLabel';
import Stepper from '@mui/material/Stepper';
import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import PAGES from '~/utils/pages';
import TutorInformation from './TutorInformation';
import TutorIntroduction from './TutorIntroduction';
import WorkInfo from './WorkInfo';
import CompleteRegistration from './CompleteRegistration';

const steps = ['Thông tin cá nhân', 'Thông tin gia sư', 'Bằng cấp / chứng chỉ'];
function TutorRegistration() {
    const [activeStep, setActiveStep] = React.useState(0);
    const [tutorInformation, setTutorInformation] = useState(null);
    const [certificate, setCertificate] = useState([]);
    const [IdVerification, setIdVerification] = useState(null);
    const [career, setCareer] = useState([]);
    const [tutorIntroduction, setTutorIntroduction] = useState(null);
    const [isSubmit, setIsSubmit] = useState(false);

    useEffect(() => {
        const draftData = localStorage.getItem(`draftData`);
        if (draftData) {
            const convertData = JSON.parse(draftData);
            setTutorInformation(convertData.tutorInformation);
            setTutorIntroduction(convertData.tutorIntroduction);
            setCareer(convertData.career);
        }
    }, [])
    useEffect(() => {
        const saveDraft = () => {
            if (!isSubmit) {
                const { image, ...draftInformation } = tutorInformation
                const draftData = {
                    tutorInformation: draftInformation,
                    tutorIntroduction: tutorIntroduction,
                    career: career
                }
                localStorage.setItem(`draftData`, JSON.stringify(draftData))
            }
        };

        window.addEventListener('beforeunload', saveDraft);
        return () => {
            window.removeEventListener('beforeunload', saveDraft);
        };
    }, [tutorInformation, tutorIntroduction, career])
    const handleNext = () => {
        setActiveStep((prevActiveStep) => prevActiveStep + 1);
    };

    const handleBack = () => {
        setActiveStep((prevActiveStep) => prevActiveStep - 1);
    };

    return (
        <Stack direction="row" sx={{ justifyContent: "center" }}>
            <Box sx={{
                width: {
                    lg: "75%"
                },
                mt: "50px"
            }}>
                <Typography variant='h3' textAlign="center" mb={5}>ĐĂNG KÝ TRỞ THÀNH GIA SƯ</Typography>
                <Breadcrumbs aria-label="breadcrumb">
                    <Link underline="hover" color="inherit" to={PAGES.ROOT + PAGES.HOME}>
                        Trang chủ
                    </Link>
                    <Typography sx={{ color: 'text.primary' }}>Đăng ký thành gia sư</Typography>
                </Breadcrumbs>
                <Paper sx={{ width: "100%", px: "40px", py: "50px", mt: "20px" }} elevation={2}>
                    <Box width="100%" px="100px">
                        <Stepper activeStep={activeStep}>
                            {steps.map((label, index) => {
                                const stepProps = {};
                                const labelProps = {};
                                return (
                                    <Step key={label} {...stepProps}>
                                        <StepLabel {...labelProps}>{label}</StepLabel>
                                    </Step>
                                );
                            })}
                        </Stepper>
                    </Box>
                    <Divider sx={{ mt: "30px" }} />
                    {activeStep === steps.length ? (
                        <CompleteRegistration />
                    ) : (
                        <React.Fragment>
                            {
                                activeStep + 1 === 1 && <TutorInformation
                                    activeStep={activeStep}
                                    handleBack={handleBack}
                                    handleNext={handleNext}
                                    steps={steps}
                                    tutorInformation={tutorInformation}
                                    setTutorInformation={setTutorInformation}
                                    IdVerification={IdVerification}
                                    setIdVerification={setIdVerification}
                                />
                            }
                            {
                                activeStep + 1 === 2 && <TutorIntroduction
                                    activeStep={activeStep}
                                    handleBack={handleBack}
                                    handleNext={handleNext}
                                    steps={steps}
                                    tutorIntroduction={tutorIntroduction}
                                    setTutorIntroduction={setTutorIntroduction}
                                />
                            }
                            {
                                activeStep + 1 === 3 && <WorkInfo
                                    activeStep={activeStep}
                                    handleBack={handleBack}
                                    handleNext={handleNext}
                                    steps={steps}
                                    certificate={certificate}
                                    career={career}
                                    setCareer={setCareer}
                                    setCertificate={setCertificate}
                                    tutorInformation={tutorInformation}
                                    tutorIntroduction={tutorIntroduction}
                                    IdVerification={IdVerification}
                                />
                            }

                        </React.Fragment>
                    )}
                </Paper>
            </Box>
        </Stack>
    )
}

export default TutorRegistration
