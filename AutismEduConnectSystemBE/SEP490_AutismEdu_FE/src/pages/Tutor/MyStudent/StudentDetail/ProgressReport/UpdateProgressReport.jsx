import ArrowRightIcon from '@mui/icons-material/ArrowRight';
import CloseIcon from '@mui/icons-material/Close';
import DoneIcon from '@mui/icons-material/Done';
import EditNoteIcon from '@mui/icons-material/EditNote';
import ListAltIcon from '@mui/icons-material/ListAlt';
import { Box, Button, FormControl, FormHelperText, MenuItem, Modal, Select, Stack, TextField, Typography } from '@mui/material';
import { useFormik } from 'formik';
import { enqueueSnackbar } from 'notistack';
import { useEffect, useState } from 'react';
import ConfirmDialog from '~/components/ConfirmDialog';
import LoadingComponent from '~/components/LoadingComponent';
import services from '~/plugins/services';
import { format } from 'date-fns';
function UpdateProgressReport({ open, setOpen, report, progressReports, setProgressReports, setSelectedItem,
    currentReport, setCurrentReport
}) {
    const handleClose = () => setOpen(false);
    const [assessment, setAssessment] = useState([]);
    const [selectedAssessment, setSelectedAssessment] = useState([]);
    const [loading, setLoading] = useState(false);
    const [changes, setChanges] = useState(true);
    const [openConfirm, setOpenConfirm] = useState(false);
    useEffect(() => {
        if (report && open) {
            handleGetAsessment();
        } else {
            setAssessment([]);
            setSelectedAssessment([]);
            formik.resetForm();
        }
    }, [report, open])

    const handleGetAsessment = async () => {
        try {
            setLoading(true);
            await services.AssessmentManagementAPI.listAssessment((res) => {
                const filterAssessment = res.result.questions.filter((a) => {
                    const choosenAss = report.assessmentResults.find((r) => r.questionId === a.id);
                    return !!choosenAss;
                });
                setAssessment(filterAssessment);
                const initialAssessment = filterAssessment.map((r, index) => {
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
        if (!values.achieved) {
            errors.achieved = "Bắt buộc";
        } else if (values.achieved.trim().length > 1000) {
            errors.achieved = "Chỉ chứa dưới 1000 ký tự"
        }
        if (!values.failed) {
            errors.failed = "Bắt buộc";
        } else if (values.failed.trim().length > 1000) {
            errors.failed = "Chỉ chứa dưới 1000 ký tự"
        }

        if (values.noteFromTutor.trim().length > 1000) {
            errors.noteFromTutor = "Chỉ chứa dưới 1000 ký tự"
        }
        return errors;
    }
    const formik = useFormik({
        initialValues: {
            achieved: '',
            failed: '',
            noteFromTutor: ''
        },
        validate,
        onSubmit: async (values) => {
            setOpenConfirm(true);
        }
    })

    const handleSubmit = async () => {
        const submitArr = selectedAssessment.map((a) => {
            const ass = report.assessmentResults.find((c) => {
                return a.questionId === c.questionId;
            })
            return {
                id: ass.id,
                questionId: a.questionId,
                optionId: a.optionId
            }
        })
        try {
            await services.ProgressReportAPI.updateProgressReport({
                achieved: formik.values.achieved.trim(),
                failed: formik.values.failed.trim(),
                noteFromTutor: formik.values.noteFromTutor.trim(),
                assessmentResults: submitArr,
                id: report.id
            }, (res) => {
                const findIndex = progressReports.findIndex((a) => {
                    return a.id === res.result.id;
                })
                if (findIndex !== -1) {
                    const updateArr = progressReports.map((p, index) => {
                        if (index !== findIndex) {
                            return p;
                        } else return res.result;
                    })
                    setProgressReports(updateArr)
                }
                if (currentReport.id === res.result.id) {
                    setCurrentReport(res.result);
                }
                handleClose();
                setOpenConfirm(false);
                setSelectedItem(res.result);
                enqueueSnackbar("Cập nhật thành công", { variant: "success" })
            }, (err) => {
                enqueueSnackbar(err.error[0], { variant: "error" })
            })
        } catch (error) {
            setLoading(false);
        }
    }
    useEffect(() => {
        if (assessment.length !== 0 && report) {
            const preData = report.assessmentResults.map((a) => {
                return {
                    questionId: a.questionId,
                    optionId: a.optionId
                }
            })
            setSelectedAssessment(preData)
            formik.resetForm({
                values: {
                    achieved: report.achieved || '',
                    failed: report.failed || '',
                    noteFromTutor: report.noteFromTutor || ''
                }
            });
        }
    }, [assessment, report])
    const formatDate = (date) => {
        if (!date) {
            return "";
        }
        const d = new Date(date);
        return `${d.getDate()}-${d.getMonth() + 1}-${d.getFullYear()}`
    }

    useEffect(() => {
        if (!report) {
            return;
        }
        if (formik.values.achieved.trim() !== report.achieved
            || formik.values.failed.trim() !== report.failed
            || formik.values.noteFromTutor.trim() !== report.noteFromTutor) {
            setChanges(false);
            return;
        } else {
            setChanges(true);
        }
        let change = true;
        if (selectedAssessment.length !== 0 && assessment.length !== 0) {
            selectedAssessment.map((s) => {
                const ass = report.assessmentResults.find((c) => {
                    return s.questionId === c.questionId;
                })
                if (ass.optionId !== s.optionId) {
                    change = false;
                }
            })
            setChanges(change)
        }
    }, [formik])
    return (
        <>
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
                        width: "60vw",
                        bgcolor: 'background.paper',
                        boxShadow: 24,
                        p: 4,
                        maxHeight: "80vh",
                        overflow: 'auto'
                    }}>
                        <form onSubmit={formik.handleSubmit}>
                            <Typography id="modal-modal-title" variant="h5" component="h2">
                                Tạo đánh giá mới
                            </Typography>
                            <Stack direction='row' alignItems='center' gap={2} mt={3}>
                                <Typography>Từ</Typography>
                                <Typography>{format(report?.from, 'dd/MM/yyyy')}</Typography>
                                <Typography>Đến</Typography>
                                <Typography>{format(report?.to, 'dd/MM/yyyy')}</Typography>
                            </Stack>

                            <Stack direction='row' gap={2} mt={2}>
                                <DoneIcon sx={{ color: "green" }} />
                                <Typography fontWeight="bold">Đã làm được</Typography>
                            </Stack>
                            <TextField multiline fullWidth minRows={5} maxRows={10}
                                name='achieved'
                                onChange={formik.handleChange}
                                value={formik.values.achieved}
                                sx={{ mt: 1 }}
                            />
                            <Stack direction='row' justifyContent='space-between'>
                                <Box>
                                    {
                                        formik.errors.achieved && (
                                            <FormHelperText error>
                                                {formik.errors.achieved}
                                            </FormHelperText>
                                        )
                                    }
                                </Box>
                                <Typography> {formik.values.achieved.length} / 1000</Typography>
                            </Stack>
                            <Stack direction='row' gap={2} mt={2}>
                                <CloseIcon sx={{ color: "red" }} />
                                <Typography fontWeight="bold">Chưa làm được</Typography>
                            </Stack>
                            <TextField multiline fullWidth minRows={5} maxRows={10}
                                name='failed'
                                onChange={formik.handleChange}
                                value={formik.values.failed}
                                sx={{ mt: 1 }} />
                            <Stack direction='row' justifyContent='space-between'>
                                <Box>
                                    {
                                        formik.errors.failed && (
                                            <FormHelperText error>
                                                {formik.errors.failed}
                                            </FormHelperText>
                                        )
                                    }
                                </Box>
                                <Typography> {formik.values.failed.length} / 1000</Typography>
                            </Stack>
                            <Stack direction='row' gap={2} mt={2}>
                                <EditNoteIcon sx={{ color: "blue" }} />
                                <Typography fontWeight="bold">Ghi chú thêm</Typography>
                            </Stack>
                            <TextField multiline fullWidth minRows={5} maxRows={10}
                                name='noteFromTutor'
                                onChange={formik.handleChange}
                                value={formik.values.noteFromTutor}
                                sx={{ mt: 1 }}
                            />
                            <Stack direction='row' justifyContent='space-between'>
                                <Box>
                                    {
                                        formik.errors.noteFromTutor && (
                                            <FormHelperText error>
                                                {formik.errors.noteFromTutor}
                                            </FormHelperText>
                                        )
                                    }
                                </Box>
                                <Typography> {formik.values.noteFromTutor.length} / 1000</Typography>
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
                                                    <Typography>{a.question}</Typography>
                                                    <FormControl size='small' sx={{ width: "300px" }} key={a.id}>
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
                                <Button variant='contained' sx={{ mt: 5 }} type='submit'
                                    disabled={changes}
                                >Lưu thay đổi</Button>
                                <Button sx={{ mt: 5 }} onClick={handleClose}>Huỷ</Button>
                            </Box>
                        </form>

                        <LoadingComponent open={loading} />
                        <ConfirmDialog openConfirm={openConfirm} setOpenConfirm={setOpenConfirm}
                            title={'Cập nhật sổ liên lạc'}
                            content={'Kiểm tra kỹ trước khi cập nhật! Bạn có muốn cập nhật sổ liên lạc này?'}
                            handleAction={handleSubmit}
                        />
                    </Box>
                </Modal>
            }
        </ >
    )
}

export default UpdateProgressReport
