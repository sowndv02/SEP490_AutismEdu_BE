import { Box, Button, Dialog, DialogContent, Grid, IconButton, List, ListItemButton, ListItemIcon, ListItemText, ListSubheader, MenuItem, Select, Stack, TextField, Typography } from '@mui/material'
import { useFormik } from 'formik';
import React, { useEffect, useRef, useState } from 'react'
import RemoveRedEyeIcon from '@mui/icons-material/RemoveRedEye';
import ImageGallery from '~/components/ImageGallery';
import AddCircleOutlineIcon from '@mui/icons-material/AddCircleOutline';
import DeleteIcon from '@mui/icons-material/Delete';
import SchoolIcon from '@mui/icons-material/School';
function Certificate({ activeStep, handleBack, handleNext, steps }) {
    const image = useRef(null);
    const [file, setFile] = useState(null);
    const validate = values => {
        const errors = {};
        return errors;
    };
    const [open, setOpen] = useState(false);
    const handleClose = () => {
        setOpen(false);
    };

    useEffect(() => {
        if (file) {
            image.current.src = URL.createObjectURL(file)
        }
    }, [file])
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
    return (
        <>
            <form onSubmit={formik.handleSubmit}>
                <Box container px="100px" py="50px" columnSpacing={2} rowSpacing={3}>
                    <Box>
                        <List
                            sx={{ maxWidth: 450, bgcolor: 'background.paper' }}
                            component="nav"
                            aria-labelledby="nested-list-subheader"
                            subheader={
                                <ListSubheader component="div" id="nested-list-subheader">
                                    <Stack direction="row" sx={{ alignItems: "center" }} gap={3}>
                                        <Typography variant='h6'>Thêm bằng cấp hoặc chứng chỉ</Typography>
                                        <IconButton><AddCircleOutlineIcon /></IconButton>
                                    </Stack>
                                </ListSubheader>
                            }
                        >
                            <ListItemButton>
                                <ListItemIcon>
                                    <SchoolIcon />
                                </ListItemIcon>
                                <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between", flexGrow: 1 }} gap={2}>
                                    <Typography >Bằng tốt nghiệp đại học FPT</Typography>
                                    <IconButton><DeleteIcon /></IconButton>
                                </Stack>
                            </ListItemButton>
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
                    <Button onClick={handleNext}>
                        {activeStep === steps.length - 1 ? 'Finish' : 'Next'}
                    </Button>
                </Box>
            </form>
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

export default Certificate
