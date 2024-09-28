import { Box, Breadcrumbs, Divider, Paper, Stack, Typography } from '@mui/material'
import React, { useEffect } from 'react'
import { Link } from 'react-router-dom'
import PAGES from '~/utils/pages'
import Stepper from '@mui/material/Stepper';
import Step from '@mui/material/Step';
import StepLabel from '@mui/material/StepLabel';
import Button from '@mui/material/Button';
import TutorInformation from './TutorInformation';
import Certificate from './Certificate';
import Identification from './Identification';

const steps = ['Thông tin gia sư', 'Bằng tốt nghiệp', 'Định danh'];
function TutorRegistration() {
    const [activeStep, setActiveStep] = React.useState(0);

    useEffect(() => {
        const handleBeforeUnload = (event) => {
            localStorage.setItem("Check", "Hello")
            event.returnValue = '';
        };

        window.addEventListener('beforeunload', handleBeforeUnload);
        return () => {
            window.removeEventListener('beforeunload', handleBeforeUnload);
        };
    }, [])
    const handleNext = () => {
        setActiveStep((prevActiveStep) => prevActiveStep + 1);
    };

    const handleBack = () => {
        setActiveStep((prevActiveStep) => prevActiveStep - 1);
    };

    const handleReset = () => {
        setActiveStep(0);
    };
    return (
        <Stack direction="row" sx={{ justifyContent: "center" }}>
            <Box sx={{
                width: {
                    lg: "75%"
                },
                mt: "50px"
            }}>
                <Breadcrumbs aria-label="breadcrumb">
                    <Link underline="hover" color="inherit" to={PAGES.ROOT + PAGES.HOME}>
                        Trang chủ
                    </Link>
                    <Typography sx={{ color: 'text.primary' }}>Đăng ký thành gia sư</Typography>
                </Breadcrumbs>
                <Paper sx={{ width: "100%", py: "50px", px: "40px", mt: "20px" }} elevation='2'>
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
                        <React.Fragment>
                            <Typography sx={{ mt: 2, mb: 1 }}>
                                All steps completed - you&apos;re finished
                            </Typography>
                            <Box sx={{ display: 'flex', flexDirection: 'row', pt: 2 }}>
                                <Box sx={{ flex: '1 1 auto' }} />
                                <Button onClick={handleReset}>Reset</Button>
                            </Box>
                        </React.Fragment>
                    ) : (
                        <React.Fragment>
                            {
                                activeStep + 1 === 1 && <TutorInformation
                                    activeStep={activeStep}
                                    handleBack={handleBack}
                                    handleNext={handleNext}
                                    steps={steps}
                                />
                            }
                            {
                                activeStep + 1 === 2 && <Certificate
                                    activeStep={activeStep}
                                    handleBack={handleBack}
                                    handleNext={handleNext}
                                    steps={steps}
                                />
                            }
                            {
                                activeStep + 1 === 3 && <Identification
                                    activeStep={activeStep}
                                    handleBack={handleBack}
                                    handleNext={handleNext}
                                    steps={steps}
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
