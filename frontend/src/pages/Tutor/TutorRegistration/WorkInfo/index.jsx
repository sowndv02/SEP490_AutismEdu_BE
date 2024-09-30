import DeleteIcon from '@mui/icons-material/Delete';
import SchoolIcon from '@mui/icons-material/School';
import WorkIcon from '@mui/icons-material/Work';
import { Box, Button, Dialog, DialogContent, IconButton, List, ListItem, ListItemButton, ListItemIcon, ListSubheader, Stack, Typography } from '@mui/material';
import { useFormik } from 'formik';
import { useEffect, useRef, useState } from 'react';
import Career from './Career';
import CertificateAddition from './Certificate/CertificateAddition';
import ConfirmDeleteDialog from './Certificate/ConfirmDeleteDialog';
import CertificateDetail from './Certificate/CertificateDetail';
import CareerDetail from './Career/CareerDetail';
import { enqueueSnackbar } from 'notistack';
function WorkInfo({ activeStep, handleBack, handleNext, steps, certificate, career, setCareer, setCertificate }) {
    const image = useRef(null);
    const validate = values => {
        const errors = {};
        return errors;
    };
    const [open, setOpen] = useState(false);
    const handleClose = () => {
        setOpen(false);
    };

    const formik = useFormik({
        initialValues: {
            university: '',
            degreeDate: '',
            file: ''
        },
        validate,
        onSubmit: async (values) => {
            console.log(values);
        }
    });
    console.log(career);
    return (
        <>
            <form onSubmit={formik.handleSubmit}>
                <Box>
                    <Box>
                        <List
                            sx={{ maxWidth: 450, bgcolor: 'background.paper' }}
                            component="nav"
                            aria-labelledby="nested-list-subheader"
                            subheader={
                                <ListSubheader component="div" id="nested-list-subheader">
                                    <Stack direction="row" sx={{ alignItems: "center" }} gap={3}>
                                        <Typography variant='h6'>Thêm bằng cấp hoặc chứng chỉ</Typography>
                                        <CertificateAddition certificate={certificate} setCertificate={setCertificate} />
                                    </Stack>
                                </ListSubheader>
                            }
                        >
                            {
                                certificate === null || certificate.length === 0 ? (
                                    <ListItem>Chưa có bằng cấp hay chứng chỉ nào</ListItem>
                                ) : (
                                    certificate?.map((c, index) => {
                                        return (
                                            <CertificateDetail key={index} index={index} currentItem={c} certificate={certificate}
                                                setCertificate={setCertificate} />
                                        )
                                    })
                                )
                            }
                        </List>
                    </Box>
                    <Box>
                        <List
                            sx={{ maxWidth: 450, bgcolor: 'background.paper' }}
                            component="nav"
                            aria-labelledby="nested-list-subheader"
                            subheader={
                                <ListSubheader component="div" id="nested-list-subheader">
                                    <Stack direction="row" sx={{ alignItems: "center" }} gap={3}>
                                        <Typography variant='h6'>Thêm kinh nghiệm làm việc</Typography>
                                        <Career career={career} setCareer={setCareer} />
                                    </Stack>
                                </ListSubheader>
                            }
                        >
                            {
                                career === null || career.length === 0 ? (
                                    <ListItem>Chưa có kinh nghiệm làm việc nào</ListItem>
                                ) : (

                                    career?.map((c, index) => {
                                        return (
                                            <CareerDetail key={index} currentItem={c} career={career} setCareer={setCareer}
                                                index={index} />
                                        )
                                    })
                                )
                            }
                        </List>
                    </Box>
                </Box>
                <Box sx={{ display: 'flex', flexDirection: 'row', pt: 2 }}>
                    <Button
                        color="inherit"
                        disabled={activeStep === 0}
                        onClick={handleBack}
                        sx={{ mr: 1 }}
                    >
                        Back
                    </Button>
                    <Box sx={{ flex: '1 1 auto' }} />
                    <Button onClick={() => {
                        if (certificate?.length !== 0) {
                            handleNext();
                        } else {
                            enqueueSnackbar("Bạn chưa có bằng cấp/ chứng chỉ", { variant: "error" })
                        }
                    }}>
                        {activeStep === steps.length - 1 ? 'Finish' : 'Next'}
                    </Button>
                </Box>
            </form >
            {
                image && (
                    <Dialog open={open} onClose={handleClose}>
                        <DialogContent style={{ textAlign: 'center' }}>
                            <img src={image.current?.src} style={{ maxHeight: "500px", minHeight: "400px", maxWidth: "100%" }} />
                        </DialogContent>
                    </Dialog>
                )
            }
        </>
    )
}

export default WorkInfo
