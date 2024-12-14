import ArrowRightIcon from '@mui/icons-material/ArrowRight';
import DoneIcon from '@mui/icons-material/Done';
import ListAltIcon from '@mui/icons-material/ListAlt';
import { Box, Button, FormControl, FormHelperText, MenuItem, Modal, Select, Stack, TextField, Typography } from '@mui/material';
import { useFormik } from 'formik';
import { enqueueSnackbar } from 'notistack';
import { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useParams } from 'react-router-dom';
import ConfirmDialog from '~/components/ConfirmDialog';
import LoadingComponent from '~/components/LoadingComponent';
import services from '~/plugins/services';
import { listStudent, setListStudent } from '~/redux/features/listStudent';
function CompleteTutoring({ studentProfile, setStudentProfile }) {
    const [open, setOpen] = useState();
    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);
    const [assessment, setAssessment] = useState([]);
    const [selectedAssessment, setSelectedAssessment] = useState([]);
    const [loading, setLoading] = useState(false);
    const [finalAssessment, setFinalAssessment] = useState(null);
    const { id } = useParams();
    const [openConfirm, setOpenConfirm] = useState(false);
    const dispatch = useDispatch();
    const listStudentProfiles = useSelector(listStudent);
    useEffect(() => {
        handleGetAsessment();
        handleGetLastProgressReport();
    }, [])
    useEffect(() => {
        if (open) {
            formik.resetForm();
        }
    }, [open])

    const handleGetLastProgressReport = async () => {
        try {
            setLoading(true);
            await services.ProgressReportAPI.getListProgressReport((res) => {
                setFinalAssessment(res.result[0]);
            }, (err) => {
                console.log(err);
            }, {
                studentProfileId: id,
                pageNumber: 1,
                orderBy: "dateFrom",
                sort: "desc"
            })
            setLoading(false);
        } catch (error) {
            setLoading(false)
        }
    }

    useEffect(() => {
        if (assessment && finalAssessment) {
            const preData = assessment.map((a) => {
                const choosenAss = finalAssessment.assessmentResults.find((r) => r.questionId === a.id);
                if (choosenAss) {
                    return {
                        questionId: choosenAss.questionId,
                        optionId: choosenAss.optionId
                    }
                } else {
                    return {
                        questionId: a.id,
                        optionId: a.assessmentOptions[0].id
                    }
                }
            });
            setSelectedAssessment(preData)
        }
    }, [assessment, finalAssessment])
    const handleGetAsessment = async () => {
        try {
            setLoading(true);
            await services.AssessmentManagementAPI.listAssessment((res) => {
                setAssessment(res.result.questions);
                const initialAssessment = res.result.questions.map((r, index) => {
                    return {
                        questionId: r.id,
                        optionId: r.assessmentOptions[0].id
                    }
                })
                setSelectedAssessment(initialAssessment)
            }, (err) => {
                console.log(err);
            })
            setLoading(false);
        } catch (error) {
            setLoading(false);
        }
    }

    const validate = (values) => {
        const errors = {};
        if (!values.finalCondition) {
            errors.finalCondition = "Bắt buộc";
        } else if (values.finalCondition.length > 1000) {
            errors.finalCondition = "Chỉ chứa dưới 1000 ký tự";
        }
        return errors;
    }
    const formik = useFormik({
        initialValues: {
            finalCondition: ""
        },
        validate,
        onSubmit: async (values) => {
            setOpenConfirm(true);
        }
    })

    const handleSubmit = async () => {
        try {
            setLoading(true);
            await services.StudentProfileAPI.closeTutoring({
                finalCondition: formik.values.finalCondition.trim(),
                studentProfileId: id,
                finalAssessmentResults: selectedAssessment
            }, (res) => {
                enqueueSnackbar("Kết thúc việc dạy thành công!", { variant: "success" })
                const filterArr = listStudentProfiles.filter((a) => {
                    return a.id !== res.result.id
                })
                dispatch(setListStudent(filterArr))
                handleClose();
                setOpenConfirm(false);
                setStudentProfile(res.result);
            }, (err) => {
                enqueueSnackbar(err.error[0], { variant: "error" })
            })
            setLoading(false);
        } catch (error) {
            setLoading(false);
        }
    }
    return (
        <>
            <Button
                variant="contained"
                color="success"
                onClick={handleOpen}
                sx={{
                    width: "200px",
                    height: "60px",
                    fontSize: "18px",
                    fontWeight: "bold",
                    borderRadius: "12px",
                    boxShadow: "0px 4px 12px rgba(0, 0, 0, 0.1)",
                    background: "linear-gradient(45deg, #3c4ff4, #b660ec)",
                    transition: "all 0.3s ease-in-out",
                    "&:hover": {
                        background: "linear-gradient(45deg, #b660ec, #3c4ff4)",
                        transform: "scale(1.05)",
                    }
                }}
            >
                Kết thúc dạy
            </Button>

            {
                open && <Modal
                    open={open}
                    onClose={handleClose}
                >
                    <Box sx={{
                        position: 'absolute',
                        top: '50%',
                        left: '50%',
                        transform: 'translate(-50%, -50%)',
                        width: '60vw',
                        bgcolor: 'background.paper',
                        boxShadow: 24,
                        p: 4,
                        maxHeight: "80vh",
                        overflow: 'auto'
                    }}>
                        <Stack direction='row' gap={2}>
                            <form onSubmit={formik.handleSubmit}>
                                <Typography id="modal-modal-title" variant="h5" component="h2">
                                    Kết thúc dạy học sinh {studentProfile.name}
                                </Typography>

                                <Stack direction='row' gap={2} mt={2}>
                                    <DoneIcon sx={{ color: "green" }} />
                                    <Typography fontWeight="bold">Đánh giá cuối cùng</Typography>
                                </Stack>
                                <TextField multiline fullWidth minRows={5} maxRows={10}
                                    name='finalCondition'
                                    onChange={formik.handleChange}
                                    value={formik.values.finalCondition}
                                    sx={{ mt: 1 }}
                                />
                                <Stack direction='row' justifyContent='space-between'>
                                    <Box>
                                        {
                                            formik.errors.finalCondition && (
                                                <FormHelperText error>
                                                    {formik.errors.finalCondition}
                                                </FormHelperText>
                                            )
                                        }
                                    </Box>
                                    <Typography> {formik.values.finalCondition.length} / 1000</Typography>
                                </Stack>
                                <Stack direction='row' gap={2} mt={2}>
                                    <ListAltIcon sx={{ color: "orange" }} />
                                    <Typography>Danh sách đánh giá</Typography>
                                </Stack>
                                <Stack direction='row' sx={{ width: "100%" }} flexWrap='wrap' rowGap={2}>
                                    {
                                        assessment && assessment.map((a, index) => {
                                            return (
                                                <Box sx={{ display: "flex", width: "50%" }} key={a.id}>
                                                    <ArrowRightIcon sx={{ fontSize: "40px", color: "red" }} />
                                                    <Box>
                                                        <Typography sx={{ width: "300px" }}>{a.question}</Typography>
                                                        <FormControl size='small' sx={{ width: "300px", mt: 1 }} key={a.id}>
                                                            <Select value={selectedAssessment[index].optionId}
                                                                onChange={(e) => {
                                                                    selectedAssessment[index].optionId = Number(e.target.value);
                                                                    setSelectedAssessment([...selectedAssessment]);
                                                                }}
                                                            >
                                                                {
                                                                    a.assessmentOptions.map((option) => {
                                                                        return (
                                                                            <MenuItem value={option.id} key={option.id}>{option.point} điểm</MenuItem>
                                                                        )
                                                                    })
                                                                }
                                                            </Select>
                                                        </FormControl>
                                                    </Box>
                                                </Box>
                                            )
                                        })
                                    }
                                </Stack>
                                <Box sx={{ display: "flex", gap: 1 }}>
                                    <Button variant='contained' sx={{ mt: 5 }} type='submit'>Kết thúc</Button>
                                    <Button sx={{ mt: 5 }} onClick={handleClose}>Huỷ</Button>
                                </Box>
                            </form>
                        </Stack>
                        <LoadingComponent open={loading} />
                        <ConfirmDialog openConfirm={openConfirm} setOpenConfirm={setOpenConfirm} handleAction={handleSubmit}
                            title={"Kết thúc lớp học"} content={`Bạn có muốn kết thúc việc dạy học ${studentProfile?.name}`} />
                    </Box>
                </Modal>
            }
        </ >
    )
}

export default CompleteTutoring
